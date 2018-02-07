pipeline {
    agent any

    stages {
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
    post {
        // always {}
        success {
            slackSend (color: '#008000', message: "Built OK: ${env.JOB_NAME} <${env.BUILD_URL}|#${env.BUILD_NUMBER}>")
        }
        // unstable {}
        failure {
            slackSend (color: '#800000', message: """Build Failed: ${env.JOB_NAME} <${env.BUILD_URL}|#${env.BUILD_NUMBER}>
Commit SHA: ${env.GIT_COMMIT}""")
        }
        // changed {}
    }
}
