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
        always {
            step([$class: 'XUnitPublisher', testTimeMargin: '3000', thresholdMode: 1, thresholds: [[$class: 'FailedThreshold', failureNewThreshold: '', failureThreshold: '', unstableNewThreshold: '', unstableThreshold: ''], [$class: 'SkippedThreshold', failureNewThreshold: '', failureThreshold: '', unstableNewThreshold: '', unstableThreshold: '']], tools: [[$class: 'XUnitDotNetTestType', deleteOutputFiles: true, failIfNotNew: true, pattern: '**/testresults.xml', skipNoTestFiles: true, stopProcessingIfError: true]]])
        }
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