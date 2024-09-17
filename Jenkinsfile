pipeline {
    agent any
    
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
    }
    post {
    success {
        githubNotify state: 'SUCCESS', context: 'continuous-integration/jenkins', description: 'Build successful'
    }
    failure {
        githubNotify state: 'FAILURE', context: 'continuous-integration/jenkins', description: 'Build failed'
    }
}
}


