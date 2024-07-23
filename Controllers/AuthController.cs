
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using TNG.Shared.Lib.Intefaces;
using Microsoft.AspNetCore.Mvc;
using TNG.Shared.Lib.Models.Auth;
using TNG.Shared.Lib.Mongo.Common;
using TNG.Shared.Lib.Mongo.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using ILogger = TNG.Shared.Lib.Intefaces.ILogger;
namespace TNG.Shared.Lib.Mongo.Master;
[Route("api/[controller]/[action]")]
[ApiController]
public class AuthController : ControllerBase
{
  /// <summary>
  /// DB Context 
  /// </summary>
  /// <value></value>
  private IMongoLayer _db { get; set; }

  /// <summary>
  /// Authentication Service
  /// </summary>
  // /// <value></value>
  private IAuthenticationService _authenticationService { get; set; }


  /// <summary>
  /// Http context
  /// </summary>
  /// <value></value>
  private IEmailer _emailer { get; set; }

  /// <summary>
  /// Logging service
  /// </summary>
  /// <value></value>
  private ILogger _logger { get; set; }
  /// <summary>
  /// Constructor
  /// </summary>
  public AuthController(IMongoLayer db, IAuthenticationService authService, IEmailer emailer, ILogger logger)
  {
    this._db = db;
    this._authenticationService = authService;
    this._emailer = emailer;
    this._logger = logger;
  }

  #region  Public Methods
  [HttpPost]
  public ActionResult<UserValidationResult> AuthenticateUsers(UserValidateRequest userToValidate)
  {
    var result = new UserValidationResult();
    var ex = new Exception();
    try
    {
      var userFilter = Builders<MDBL_User>.Filter.Eq(x => x.Email, userToValidate.Email.Trim().ToLower())
                      & Builders<MDBL_User>.Filter.Eq(x => x.IsActive, true);
      var user = this._db.LoadDocuments(MONGO_MODELS.USER, userFilter).FirstOrDefault();

      if (user == null)
      {
        result.Error = ERROR_SETTINGS.USER_NOT_FOUND;
        result.Success = false;
        result.Token = null;
        return result;
      }

      var settings = this._db.LoadAll<MDBL_Settings>(MONGO_MODELS.SETTINGS);

      var inputPasswordHash = this.hashPassword(userToValidate.Password, user.Salt);
      var expirySettingVal = settings.Where(setting => setting.Key == ACCOUNTS_SETTING_MASTER.USER_KEY_VALIDITY_MINS).FirstOrDefault();

      if (user != null && user.IsActive == true && user.IsDeleted == false && user.IsLocked == false)
      {

        if (user.Password.Equals(inputPasswordHash))
        {
          UserValidationResult authResult = getUserToken(user, expirySettingVal);
          user.LastLogIn = DateTime.UtcNow;
          user.UpdatedDate = DateTime.UtcNow;
          this._db.UpdateDocument(MONGO_MODELS.USER, user);
          return authResult;
        }
        else
        {
          var loginAttempt = user.LoginAttempts ?? 0;
          user.LoginAttempts = ++loginAttempt;
          if (user.LoginAttempts >= 10)
          {
            user.IsLocked = true;
          }

          this._db.UpdateDocument(MONGO_MODELS.USER, user);
          this.returnInvalidAuth(result);
        }

      }
      else if (user != null && user.IsDeleted == false && user.IsActive == false)
      {
        result.Error = ERROR_SETTINGS.USER_HAS_BEEN_LOCKED_BY_ADMIN;
        result.Success = false;
        result.Token = null;
        return result;
      }
      else if (user != null && user.IsDeleted == false && user.IsLocked == true)
      {
        result.Error = ERROR_SETTINGS.LOGIN_LIMIT_EXCEEDED;
        result.Success = false;
        result.Token = null;
        return result;
      }
      else
      {
        this.returnInvalidAuth(result);
      }


    }
    catch (Exception ec)
    {
      this._logger.LogError("AUTHCONTROLLER", "AuthenticateUser", ec.Message, this.HttpContext.Connection.RemoteIpAddress.ToString());
      this.returnInvalidAuth(result);
    }
    return result;
  }
  /// <summary>
  /// To generate refresh token
  /// </summary>
  /// <param name="refreshData"></param>
  /// <returns></returns>

  [HttpPost]
  public ActionResult<AuthTokenRefreshModel> RefreshToken(UserTokenRefreshInputModel refreshData)
  {

    //add final refresh token
    var tokenRefreshResult = new AuthTokenRefreshModel { Success = false };
    var authtoken = refreshData.AuthToken;
    try
    {

      var authTokenString = this._authenticationService.Decrypt(authtoken);
      var userToken = JsonConvert.DeserializeObject<UserToken>(authTokenString);

      var session = this._db.LoadDocumentById<MDBL_UserSession>(MONGO_MODELS.USERSESSION, userToken.TokenId);
      var user = this._db.LoadDocumentById<MDBL_User>(MONGO_MODELS.USER, userToken.Id);
      var settings = this._db.LoadAll<MDBL_Settings>(MONGO_MODELS.SETTINGS);


      if (session != null && session.IsActive == true && user != null && user.IsActive == true && user.IsLocked == false)
      {
        var expirySettingVal = settings.Where(setting => setting.Key == ACCOUNTS_SETTING_MASTER.USER_KEY_VALIDITY_MINS).FirstOrDefault();
        var ip = this.HttpContext.Connection.RemoteIpAddress.ToString();
        userToken.Expiry = DateTime.UtcNow.AddMinutes(Convert.ToInt32(expirySettingVal.Value));

        // Increasing count
        session.RefreshCount++;

        var refreshedTokenString = JsonConvert.SerializeObject(userToken);
        session.LastIssuedRefreshTokenObject = refreshedTokenString;
        this._db.UpdateDocument(MONGO_MODELS.USERSESSION, session);

        var encryptedToken = this._authenticationService.Encrypt(refreshedTokenString);
        tokenRefreshResult.Success = true;
        tokenRefreshResult.Token = encryptedToken;
      }
      else
      {
        tokenRefreshResult.IsReAuthRequired = true;

      }

    }
    catch (Exception ex)
    {

      this._logger.LogError("AUTHCONTROLLER", "RefreshToken", ex.Message, this.HttpContext.Connection.RemoteIpAddress.ToString());
      tokenRefreshResult.IsReAuthRequired = true;
    }
    return tokenRefreshResult;
  }

  /// <summary>
  /// To change a user's password using forget password
  /// </summary>
  /// <returns></returns>

  [HttpPost]
  public ActionResult<bool> ForgetPassword(ChangePasswordModel newCredential)
  {

    try
    {
      if (String.Equals(newCredential.ConfirmPassword, newCredential.Password))
      {
        var userFilter = Builders<MDBL_User>.Filter.Eq(x => x.UserId, newCredential.UserId)
            & Builders<MDBL_User>.Filter.Eq(x => x.IsActive, true);
        var dbUser = this._db.LoadDocuments(MONGO_MODELS.USER, userFilter).FirstOrDefault();

        var userSessionFilter = Builders<MDBL_UserSession>.Filter.Eq(x => x.UserId, dbUser.UserId)
            & Builders<MDBL_UserSession>.Filter.Eq(x => x.IsActive, true);
        var userSessions = this._db.LoadDocuments(MONGO_MODELS.USERSESSION, userSessionFilter);

        if (dbUser != null)
        {
          dbUser.IsLocked = false;
          dbUser.LoginAttempts = 0;
          foreach (var session in userSessions)
          {
            session.SessionEnd = DateTime.UtcNow;
            session.IsActive = false;
            this._db.UpdateDocument(MONGO_MODELS.USERSESSION, session);
          }
          if (dbUser.Salt == null)
          {
            dbUser.Salt = Guid.NewGuid().ToString().Trim().Replace("-", string.Empty);
            // this._db.UpdateDocument(MONGO_MODELS.USER, dbUser);
          }

          dbUser.Password = this.hashPassword(newCredential.Password, dbUser.Salt);
          this._db.UpdateDocument(MONGO_MODELS.USER, dbUser);

          return true;
        }
      }
    }
    catch (Exception ex)
    {
      this._logger.LogError("AUTHCONTROLLER", "ForgetPassword", $"User:{newCredential.UserId}_{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
    }
    return false;
  }

  [TNGAuthAttribute($"{ACCOUNTS_USERTYPE_MASTER.CLIENT},{ACCOUNTS_USERTYPE_MASTER.MASTER},{ACCOUNTS_USERTYPE_MASTER.ADMIN}")]
  [HttpPost]
  public ActionResult<FacilityResponse> ChangePassword(ChangeUserPasswordRequest request)
  {
    var response = changePassword(request);
    return response;

  }
  #endregion

  #region  Private Helpers
  /// <summary>
  /// Hash a password
  /// </summary>
  /// <param name="password"></param>
  /// <param name="salt"></param>
  /// <returns></returns>
  private string hashPassword(string password, string salt)
  {
    return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                        password: password,
                        salt: Convert.FromBase64String(salt),
                        prf: KeyDerivationPrf.HMACSHA1,
                        iterationCount: 10000,
                        numBytesRequested: 256 / 8));
  }
  private void returnInvalidAuth(UserValidationResult result)
  {
    result.Error = ERROR_SETTINGS.INVALID_LOGIN;
    result.Success = false;
    result.Token = null;
  }


  /// <summary>
  /// create and sent user token
  /// </summary>
  /// <param name="user"></param>
  /// <param name="expirySettingVal"></param>
  /// <returns></returns>
  private UserValidationResult getUserToken(MDBL_User user, MDBL_Settings expirySettingVal)
  {
    //Create and send the token 
    var userToken = new UserToken
    {
      UserId = user.UserId,
      RequestIP = this.HttpContext.Connection.RemoteIpAddress.ToString(),
      Expiry = DateTime.UtcNow.AddMinutes(Convert.ToInt32(expirySettingVal.Value)),
      UserType = user.UserType,
      Id = user.Id
    };

    // Get existing sessions
    var userSessionFilter = Builders<MDBL_UserSession>.Filter.Eq(x => x.UserGuid, user.UserId)
        & Builders<MDBL_UserSession>.Filter.Eq(x => x.IsActive, true);

    var userSessions = this._db.LoadDocuments(MONGO_MODELS.USERSESSION, userSessionFilter);
    foreach (var item in userSessions)
    {
      item.SessionEnd = DateTime.UtcNow;
      item.IsActive = false;
      this._db.UpdateDocument(MONGO_MODELS.USERSESSION, item);
    }


    var userSession = new MDBL_UserSession
    {
      IsActive = true,
      SessionStart = DateTime.UtcNow,
      UserGuid = user.UserId,
      RefreshCount = 0,
      UserId = user.Id
    };

    this._db.InsertDocument(MONGO_MODELS.USERSESSION, userSession);
    userToken.TokenId = userSession.Id;
    userSession.InitialTokenObject = JsonConvert.SerializeObject(userToken);
    this._db.UpdateDocument(MONGO_MODELS.USERSESSION, userSession);

    user.LoginAttempts = 0;
    this._db.UpdateDocument(MONGO_MODELS.USER, user);


    var encryptedToken = this._authenticationService.Encrypt(JsonConvert.SerializeObject(userToken));
    var authResult = new UserValidationResult
    {
      Success = true,
      Token = encryptedToken,
      UserType = userToken.UserType,
      UserId = user.UserId,
      IsEmailVerified = user.IsEmailVerified,
      Username = user.Username,
      Email = user.Email
    };
    return authResult;
  }

  /// <summary>
  /// To change password through user profile
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>

  private FacilityResponse changePassword(ChangeUserPasswordRequest request)
  {
    var userId = this._authenticationService.User.UserId;
    FacilityResponse response = new FacilityResponse();
    try
    {
      if (String.Equals(request.ConfirmPassword, request.Password))
      {
        var userFilter = Builders<MDBL_User>.Filter.Eq(x => x.UserId, userId)
            & Builders<MDBL_User>.Filter.Eq(x => x.IsActive, true);
        var dbUser = this._db.LoadDocuments(MONGO_MODELS.USER, userFilter).FirstOrDefault();

        var userSessionFilter = Builders<MDBL_UserSession>.Filter.Eq(x => x.UserGuid, userId)
            & Builders<MDBL_UserSession>.Filter.Eq(x => x.IsActive, true);
        var userSessions = this._db.LoadDocuments(MONGO_MODELS.USERSESSION, userSessionFilter);

        if (dbUser != null)
        {
          dbUser.IsLocked = false;
          dbUser.LoginAttempts = 0;
          foreach (var session in userSessions)
          {
            session.SessionEnd = DateTime.UtcNow;
            session.IsActive = false;
            this._db.UpdateDocument(MONGO_MODELS.USERSESSION, session);
          }
            dbUser.Password = this.hashPassword(request.Password, dbUser.Salt);
            dbUser.UpdatedDate = DateTime.UtcNow;
            this._db.UpdateDocument(MONGO_MODELS.USER, dbUser);
            response.IsSuccess = true;
            response.Message = "";
            return response;
        }
        else
        {
          response.IsSuccess = false;
          response.Message = ERROR_SETTINGS.USER_NOT_FOUND;
          return response;

        }
      }
      else
      {
        response.IsSuccess = false;
        response.Message = ERROR_SETTINGS.PASSWORD_MISMATCH;
        return response;
      }
    }
    catch (Exception ex)
    {

      this._logger.LogError("AUTHCONTROLLER", "ChangePassword", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
      response.IsSuccess = false;
      response.Message = "";
      return response;

    }

  }

  #endregion


}