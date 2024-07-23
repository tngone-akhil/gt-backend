pipeline {
    agent any

    stages {
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
    }


    post {
        always {
            // Save build files to a directory and display paths
            script {
                try {
                    def workspacePath = env.WORKSPACE
                    def buildFilesDir = "${workspacePath}\\build-files" // Use double backslashes for Windows paths
                
                    // Create directory if it doesn't exist
                    if (!new File(buildFilesDir).exists()) {
                        bat "mkdir \"${buildFilesDir}\""
                    }

                    // Move .dll files to build-files directory
                  bat "move /Y \"${workspacePath}\\bin\\Release\\net8.0\\publish\\*\" \"${buildFilesDir}\""
                  bat "xcopy /Y \"${workspacePath}\\bin\\Release\\net8.0\\publish\\*\" \"${buildFilesDir}\"/E"


                    
                    // Display paths of saved files
                    echo "Build files saved in directory: ${buildFilesDir}"
                    echo "Files saved:"
                    bat "dir \"${buildFilesDir}\""
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