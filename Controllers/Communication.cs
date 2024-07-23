using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using TNG.Shared.Lib.Intefaces;
using TNG.Shared.Lib.Models.Auth;
using TNG.Shared.Lib.Mongo.Common;
using TNG.Shared.Lib.Mongo.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using ILogger = TNG.Shared.Lib.Intefaces.ILogger;
using TNG.Shared.Lib.Communications.Email;
namespace TNG.Shared.Lib.Mongo.Master;
[Route("api/akhil/[controller]/[action]")]
[ApiController]
public class CommunicationController : ControllerBase
{

    private IMongoLayer _db { get; set; }

    /// <summary>
    /// Authentication Service
    /// </summary>
    /// <value></value>
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
    public CommunicationController(IMongoLayer db, IAuthenticationService authService, IEmailer emailer, ILogger logger)
    {
        this._db = db;
        this._authenticationService = authService;
        this._emailer = emailer;
        this._logger = logger;
    }

            /// <summary>
        /// To generate verification email
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LocketResponse> GenerateVerificationEmail(GenerateEmail request)
        {
            var response = generateVerificationEmail(request);
            return response;
        }

    /// <summary>
    /// To send a request to update user password through email
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ActionResult<FacilityResponse> ChangePasswordRequest(string email)
    {
        var result = new FacilityResponse();
        var user = new MDBL_User();
        TNGEmail tNGEmail = new TNGEmail();
        try
        {
            var userFilter = Builders<MDBL_User>.Filter.Eq(x => x.Email, email);
            //Getting user 
            user = this._db.LoadDocuments(MONGO_MODELS.USER, userFilter).FirstOrDefault();
            if (user != null)
            {
                var settings = this._db.LoadAll<MDBL_Settings>(MONGO_MODELS.SETTINGS);
                MDBL_Settings changeemailTemp = new MDBL_Settings();
                MDBL_Settings changeemailLink = new MDBL_Settings();
                MDBL_Settings changeemailSub = new MDBL_Settings();
                string body = string.Empty;
                string uuid = string.Empty;
                changeemailSub = settings.Where(setting => setting.Key == TEMPLATE.CHANGEPASSWORDSUBJECT).FirstOrDefault();


                if (string.Equals(user.UserType, ACCOUNTS_USERTYPE_MASTER.MASTER))
                {
                    changeemailTemp = settings.Where(setting => setting.Key == TEMPLATE.CHANGEPASSWORDEMAILTEMPLATE).FirstOrDefault();
                    changeemailLink = settings.Where(setting => setting.Key == TEMPLATE.CHANGEPASSWORDEMAILLINK).FirstOrDefault();
                    uuid = Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                    body = changeemailTemp.Value;
                    body = body.Replace("{user}", user.Username);
                    body = body.Replace("{link}", "<a href='" + changeemailLink.Value + uuid + "' style='color: blue;'>set new password</a>");


                }
                else if (string.Equals(user.UserType, ACCOUNTS_USERTYPE_MASTER.CLIENT)||string.Equals(user.UserType,ACCOUNTS_USERTYPE_MASTER.ADMIN))
                {
                    changeemailTemp = settings.Where(setting => setting.Key == TEMPLATE.CHANGE_PASSWORD_EMAIL_TEMPLATE_FOR_USER).FirstOrDefault();
                    Random code = new Random();
                    int randomCode = code.Next(100000, 999999);
                    uuid = randomCode.ToString();

                    body = changeemailTemp.Value;
                    body = body.Replace("{name}", user.Username);
                    body = body.Replace("{code}", uuid);
                }
                var credResetRequests = new MDBL_CredResetRequests();
                credResetRequests.GeneratedDate = DateTime.UtcNow;
                credResetRequests.IPAddress = this.HttpContext.Connection.RemoteIpAddress.ToString();
                credResetRequests.UserGuid = user.UserId;
                credResetRequests.UUID = uuid;
                credResetRequests.IsExpired = false;
                credResetRequests.ToEmail = user.Email.ToString();
                credResetRequests.IsResetRequestCompleted = false;
                credResetRequests.CreatedDate = DateTime.UtcNow;
                credResetRequests.UpdatedDate = DateTime.UtcNow;
                this._db.InsertDocument(MONGO_MODELS.CREDRESETREQUESTS, credResetRequests);


                TNGEMailAddress tNGEMailAddress = new TNGEMailAddress();
                tNGEMailAddress.EmailId = user.Email;
                tNGEmail.To = tNGEMailAddress;
                tNGEmail.Content = body;
                tNGEmail.Subject = changeemailSub.Value;
                var id = addEmailLog(tNGEmail);
                this._emailer.SendMailSendGrid(tNGEmail);
                result.IsSuccess = true;
                result.Message = string.Empty;
            }
            else
            {
                result.IsSuccess = false;
                result.Message = ERROR_SETTINGS.USER_NOT_FOUND;
            }

        }
        catch (Exception ex)
        {
            this._logger.LogError("COMMUNICATIONCONTROLLER", "ChangePasswordRequest", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
            result = null;
        }
        return result;
    }


    [HttpPost]
    public ActionResult<ResetPasswordResponse> VerifyResetPasswordCode(VerifyEmailRequest request)
    {
        var response = verifyResetPasswordCode(request);
        return response;
    }
    [HttpPost]
    [TNGAuthAttribute($"{ACCOUNTS_USERTYPE_MASTER.MASTER}")]
    public FacilityResponse AddUsers(CreateUserRequest request)
    {

        FacilityResponse response = new FacilityResponse();
        try
        {
            var filter = Builders<MDBL_User>.Filter.Where(user => user.Email == request.Email);
            var user = this._db.LoadDocuments(MONGO_MODELS.USER, filter).FirstOrDefault();
            if (user == null)
            {
                var newUser = new MDBL_User();
                var salt = Guid.NewGuid().ToString().Trim().Replace("-", string.Empty);
                newUser.Email = request.Email;
                newUser.Username = request.Name;
                newUser.PhoneNumber = request.PhoneNumber;
                newUser.Location = request.Location;
                newUser.UserType = request.Role;
                newUser.IsActive = true;
                newUser.IsDeleted = false;
                newUser.IsEmailVerified = true;
                newUser.Salt = salt;
                newUser.IsLocked = false;
                newUser.UserId = Guid.NewGuid().ToString();
                var password="Asdf@123";
                newUser.Password = hashPassword(password, salt);
                newUser.CreatedDate = DateTime.Now;
                newUser.UpdatedDate = DateTime.Now;
                this._db.InsertDocument(MONGO_MODELS.USER, newUser);
                response.IsSuccess = true;
                response.Message = newUser.UserId;

            }
            else
            {
                response.IsSuccess = false;
                response.Message = ERROR_SETTINGS.EMAIL_ID_ALREADY_EXISTS;
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError("AUTHCONTROLLER", "UserCreation", ex.Message, this.HttpContext.Connection.RemoteIpAddress.ToString());
            response.IsSuccess = false;
            response.Message = string.Empty;
        }
        return response;
    }

    #region Private Helpers

    /// <summary>
    /// Private method to generate verification email
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
        private LocketResponse generateVerificationEmail(GenerateEmail request)
        {
            LocketResponse response = new LocketResponse();
            try
            {
                var userFilter = Builders<MDBL_User>.Filter.Eq(user => user.Email, request.Email);
                var user = this._db.LoadDocuments(MONGO_MODELS.USER, userFilter).FirstOrDefault();
                if (user != null)
                {
                    if (user.IsActive == false)
                    {
                        response.IsSuccess = false;
                        response.Error = ERROR_SETTINGS.USER_HAS_BEEN_LOCKED_BY_ADMIN;
                    }
                    else
                    {
                        SendEmail(user);
                        response.IsSuccess = true;
                        response.Message = string.Empty;
                    }
                }
                else
                {
                    response.IsSuccess = false;
                    response.Message = ERROR_SETTINGS.USER_NOT_FOUND;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("AUTHCONTROLLER", "GenerateVerificationEmail", ex.Message, this.HttpContext.Connection.RemoteIpAddress.ToString());
                response.IsSuccess = false;
                response.Message = string.Empty;
            }
            return response;
        }
    private ResetPasswordResponse verifyResetPasswordCode(VerifyEmailRequest request)
    {
        ResetPasswordResponse response = new ResetPasswordResponse();
        try
        {
            var userFilter = Builders<MDBL_User>.Filter.Eq(user => user.Email, request.Email);
            var user = this._db.LoadDocuments(MONGO_MODELS.USER, userFilter).FirstOrDefault();
            var credFilter = Builders<MDBL_CredResetRequests>.Filter.Eq(reset => reset.UserGuid, user.UserId) & Builders<MDBL_CredResetRequests>.Filter.Eq(reset => reset.IsResetRequestCompleted, false);
            var sort = Builders<MDBL_CredResetRequests>.Sort.Descending(date => date.CreatedDate);
            var credData = this._db.LoadDocuments(MONGO_MODELS.CREDRESETREQUESTS, credFilter, null, sort).FirstOrDefault();
            if (credData != null)
            {
                DateTime d1 = DateTime.UtcNow;
                DateTime d2 = credData.GeneratedDate.AddMinutes(+2);

                if (d1 < d2)
                {
                    if (string.Equals(credData.UUID, request.Code))
                    {
                        response.IsSuccess = true;
                        response.UserId = credData.UserGuid;
                        credData.IsResetRequestCompleted = true;
                        this._db.UpdateDocument(MONGO_MODELS.CREDRESETREQUESTS, credData);
                    }
                    else
                    {
                        response.IsSuccess = false;
                        response.Error = ERROR_SETTINGS.WRONG_CODE_ERR;
                        response.UserId = null;
                        this._db.UpdateDocument(MONGO_MODELS.CREDRESETREQUESTS, credData);
                    }

                }
                else
                {
                    response.IsSuccess = false;
                    response.Error = ERROR_SETTINGS.TIME_OUT_ERR;
                    response.UserId = null;
                    credData.IsExpired = true;
                    this._db.UpdateDocument(MONGO_MODELS.CREDRESETREQUESTS, credData);
                }
            }
            else
            {
                response.IsSuccess = false;
                response.Error = ERROR_SETTINGS.RESET_NOT_REQUESTED;
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError("AUTHCONTROLLER", "VerifyResetPasswordCode", ex.Message, this.HttpContext.Connection.RemoteIpAddress.ToString());
            response.IsSuccess = false;
            response.Error = null;
        }
        return response;
    }

    private string hashPassword(string password, string salt)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                            password: password,
                            salt: Convert.FromBase64String(salt),
                            prf: KeyDerivationPrf.HMACSHA1,
                            iterationCount: 10000,
                            numBytesRequested: 256 / 8));
    }

    /// <summary>
    /// Add Email Log
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    private string addEmailLog(TNGEmail email)
    {
        var emailLog = new MDBL_EmailLog();
        emailLog.IsHTML = email.IsHTML;
        emailLog.EmailUID = Guid.NewGuid().ToString();
        emailLog.IsSent = false;
        emailLog.IsFailed = false;
        emailLog.Subject = email.Subject;
        emailLog.Updated = DateTime.UtcNow;
        emailLog.ErrorMessage = string.Empty;
        emailLog.To = email.To.EmailId;
        emailLog.Content = email.Content;
        emailLog.CreatedDate = DateTime.UtcNow;
        emailLog.UpdatedDate = DateTime.UtcNow;
        this._db.InsertDocument(MONGO_MODELS.EMAILLOG, emailLog);
        return emailLog.Id;
    }

    /// <summary>
    /// To send otp to the user email
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private bool SendEmail(MDBL_User user)
    {
        var settings = this._db.LoadAll<MDBL_Settings>(MONGO_MODELS.SETTINGS);
        var emailTemp = settings.Where(setting => setting.Key == TEMPLATE.VERIFICATION_EMAIL_TEMPLATE).FirstOrDefault();
        var emailSub = settings.Where(setting => setting.Key == TEMPLATE.VERIFICATION_EMAIL_SUBJECT).FirstOrDefault();
        string body = emailTemp.Value;
        var default_password="q13A34jkl";
        //using streamreader for reading my htmltemplate   
        body = body.Replace("{name}", user.Username); //replacing the required things 
        body = body.Replace("{username}", user.Email);
        body = body.Replace("{default_password}", default_password.ToString()); //replacing the required things  
        var tngMail = new TNGEmail
        {
            Content = body,
            To = new TNGEMailAddress { EmailId = user.Email },
            Subject = emailSub.Value
        };
        var emailid = addEmailLog(tngMail);
        var isSent = true;
        string emailErrorMessage = string.Empty;
        try
        {
            isSent = this._emailer.SendMailSendGrid(tngMail);
            var emailLog = this._db.LoadDocumentById<MDBL_EmailLog>(MONGO_MODELS.EMAILLOG, emailid);
            emailLog.IsSent = true;
            emailLog.Updated = DateTime.UtcNow;
            this._db.UpdateDocument(MONGO_MODELS.EMAILLOG, emailLog);

        }
        catch (Exception ex)
        {
            isSent = false;
            emailErrorMessage = string.Format("Subject: {0} ...", ex.Message);
            var emailLog = this._db.LoadDocumentById<MDBL_EmailLog>(MONGO_MODELS.EMAILLOG, emailid);
            emailLog.IsSent = false;
            emailLog.IsFailed = true;
            emailLog.ErrorMessage = emailErrorMessage;
            emailLog.Updated = DateTime.UtcNow;
            this._db.UpdateDocument(MONGO_MODELS.EMAILLOG, emailLog);
        }
        this._db.UpdateDocument(MONGO_MODELS.USER, user);
        return true;
    }

    #endregion
}
