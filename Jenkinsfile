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
        stage('Notify GitHub') {
            when {
                expression { currentBuild.result == 'SUCCESS' }
            }
            steps {
                script {
                    def sha = sh(script: "git rev-parse HEAD", returnStdout: true).trim()
                    def statusUrl = "https://api.github.com/repos/tngone-akhil/gt-backend/statuses/${sha}"
                    def jsonBody = new groovy.json.JsonBuilder([
                        state: 'success',
                        target_url: "${env.JENKINS_URL}/job/${env.JOB_NAME}/${env.BUILD_NUMBER}/",
                        description: "Build ${env.BUILD_NUMBER} succeeded",
                        context: "continuous-integration/jenkins"
                    ]).toPrettyString()

                    def connection = new URL(statusUrl).openConnection()
                    connection.setRequestMethod("POST")
                    connection.setRequestProperty("Authorization", "token ${env.GITHUB_TOKEN}")
                    connection.setRequestProperty("Accept", "application/vnd.github.v3+json")
                    connection.doOutput = true
                    connection.outputStream.withWriter { it.write(jsonBody) }
                    connection.inputStream.text
                }
            }
        }
         
        stage('Fail GitHub') {
            when {
                expression { currentBuild.result == 'FAILURE' }
            }
            steps {
                script {
                    def sha = sh(script: "git rev-parse HEAD", returnStdout: true).trim()
                    def statusUrl = "https://api.github.com/repos/tngone-akhil/gt-backend/statuses/${sha}"
                    def jsonBody = new groovy.json.JsonBuilder([
                        state: 'failure',
                        target_url: "${env.JENKINS_URL}/job/${env.JOB_NAME}/${env.BUILD_NUMBER}/",
                        description: "Build ${env.BUILD_NUMBER} failed",
                        context: "continuous-integration/jenkins"
                    ]).toPrettyString()

                    def connection = new URL(statusUrl).openConnection()
                    connection.setRequestMethod("POST")
                    connection.setRequestProperty("Authorization", "token ${env.GITHUB_TOKEN}")
                    connection.setRequestProperty("Accept", "application/vnd.github.v3+json")
                    connection.doOutput = true
                    connection.outputStream.withWriter { it.write(jsonBody) }
                    connection.inputStream.text
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
