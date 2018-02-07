pipeline {
    agent any

    stages {
        stage('Checkout Branch') {
            steps {
                bat 'git checkout %GIT_BRANCH%'
            }
        }
        stage('Submodules') {
            steps {
                // ToDo: if submodule out-of-date, delete build\packages dir!
                bat 'git submodule update --init'
            }
        }
        stage('Build') {
            steps {
                bat 'build/MyGet.bat'
            }
        }
    }
}
