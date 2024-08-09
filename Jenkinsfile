pipeline {
    agent any
    
    environment {
        TENANT_ID = 'c18a2dc0-ba9f-4d30-a3c0-57735c229588'
        CLIENT_ID = '2f7c4422-aba0-460e-a968-02f63cbf43b8'
        CLIENT_SECRET = '2f3f4ca9-6b01-4ee7-8d1a-df1340d1405d'
        SCOPE = 'https://graph.microsoft.com/.default'
    }
    
    stages {
        

        stage('Clone File') {
            steps {
                script {
                    // appcmd.exe stop site /site.name:"Default Web Site"
                    def gitUrl = 'https://github.com/tngone-akhil/gt-shared.git'
                    def workspacePath = env.WORKSPACE
                    def parentDir = new File(workspacePath).parent
                   def targetDir = "${parentDir}\\external"

                     if (new File(targetDir).exists()) {
                       bat "rmdir /S /Q \"${targetDir}\""
                    }
                         
                    bat "mkdir \"${targetDir}\""
                  
                    // Clone the repository and fetch only the specific file

                    bat "git clone --single-branch --branch shared ${gitUrl} ${targetDir}"
                 
                }
            }
        }

        stage('Checkout') {
            steps {
                // Checkout your Git repository
                checkout scm
            }
        }

        stage('Build') {
            steps {
                // Build the .NET project
                bat 'dotnet publish -c release'
             
                
                // Archive build artifacts
                archiveArtifacts artifacts: '**/bin/**/*.dll', allowEmptyArchive: true
            }
        }
        stage('deploy') {
            steps {
              script {
                try {   
                
                    // appcmd start sites "site1"

                    def workspacePath = env.WORKSPACE
                    def workspacePathExcept =  new File(workspacePath).parent
                    def buildFilesDir = "${workspacePathExcept}\\build-files\\1"
                      if (!new File(buildFilesDir).exists()) {
                        bat "mkdir \"${buildFilesDir}\""
                    }

                    // // Move .dll files to build-files directory
         
                     bat "scp -o StrictHostKeyChecking=no \"${workspacePath}\\bin\\Release\\net8.0\\publish\\*\" Administrator@ws5.orderstack.io:C:\\Users\\Administrator\\jenkins"
                    

                    // // Display paths of saved files
                    echo "Build files saved in directory: ${buildFilesDir}"
                    echo "Files saved:"
                    bat "dir \"${buildFilesDir}\""

                   
                    //   bat "iisreset /stop"
                } catch (Exception e) {
                    // Catch any exception and print error message
                    echo "Error in post-build actions: ${e.message}"
                    currentBuild.result = 'FAILURE' // Mark build as failure
                    throw e // Throw the exception to terminate the script
                }
            }
            }
        }
       
    }
}



// node {
//     // Define SonarScanner tool installation

//     def scannerHome = tool name: 'SonarScanner', type: 'hudson.plugins.sonar.SonarRunnerInstallation'
    
//     try {
//         // Stage: Checkout
//         stage('Checkout') {
//             // Checkout your Git repository
//             checkout scm
//             echo "${scannerHome}"
//             echo "hii"
//         }

//         // Stage: Build
//         stage('Build') {
//             // Build the .NET project
//             bat 'dotnet build bugtrackerapi.sln /p:Configuration=Release'
//               archiveArtifacts artifacts: '**/bin/**/*.dll', allowEmptyArchive: true
//         }


//         Stage: Run SonarScanner
//         stage('Run SonarScanner') {
//             // Execute SonarScanner
//             withSonarQubeEnv('SonarScanner') {
//                 bat "${scannerHome}/bin/sonar-scanner"
//             }
//         }
//     } finally {
//         // Clean up workspace
//         deleteDir()
//     }
// }
