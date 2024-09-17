pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build') {
            steps {
                bat 'echo Building...'
            }
        }

        stage('Test') {
            steps {
                bat 'echo Running Tests...'
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
