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

    
  
}
