pipeline {
    agent any
      environment {
        GITHUB_TOKEN = credentials('merge-checks')
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
       
         
      stage('Notify GitHub Checks API') {
            steps {
                script {
                    def sha = sh(script: "git rev-parse HEAD", returnStdout: true).trim()
                    def repoOwner = 'tngone-akhil'
                    def repoName = 'gt-backend'
                    def checkRunId = sh(script: "curl -s -X POST -H \"Authorization: token ${env.GITHUB_TOKEN}\" -H \"Accept: application/vnd.github.v3+json\" -d '{\"name\": \"Jenkins Build\",\"head_sha\": \"${sha}\",\"status\": \"in_progress\",\"started_at\": \"$(date --utc +%Y-%m-%dT%H:%M:%SZ)\",\"external_id\": \"${env.BUILD_NUMBER}\"}' https://api.github.com/repos/${repoOwner}/${repoName}/check-runs | jq -r '.id'", returnStdout: true).trim()

                    def status = currentBuild.result == 'SUCCESS' ? 'success' : 'failure'
                    sh "curl -s -X PATCH -H \"Authorization: token ${env.GITHUB_TOKEN}\" -H \"Accept: application/vnd.github.v3+json\" -d '{\"status\": \"${status}\",\"completed_at\": \"$(date --utc +%Y-%m-%dT%H:%M:%SZ)\",\"conclusion\": \"${status}\",\"output\": {\"title\": \"Jenkins Build\",\"summary\": \"Build ${env.BUILD_NUMBER} ${status}\"}}' https://api.github.com/repos/${repoOwner}/${repoName}/check-runs/${checkRunId}"
                }
            }
        }
     
        stage('deploy') {
            steps {
              script {
                try {   
                
                    // // Display paths of saved files
                    echo "Build files saved in directory"

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
