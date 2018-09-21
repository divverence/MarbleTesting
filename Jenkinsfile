pipeline {
    agent any

    stages {
        stage('Submodules') {
            steps {
                // ToDo: if submodule out-of-date, delete build\packages dir!
                bat 'git submodule update --init'
                // Force Develop, master to exist for gitversion to be happy - each command will fail (by design) if current branch IS that branch, hence the exit 0
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
           step([$class: 'MSTestPublisher', testResultsFile: '**/testresults*.xml', failOnError: true, keepLongStdio: true])
        }
        success {
            script {
                def tag = bat (returnStdout: true, script: '@git tag -l --points-at HEAD')
                echo "Tag: ${tag}"

                if (tag?.trim()) {
                    slackSend (color: '#008000', message: "Built OK: ${env.JOB_NAME} <${env.BUILD_URL}|#${env.BUILD_NUMBER}> `${tag.trim()}`")
                } else {
                    slackSend (color: '#008000', message: "Built OK: ${env.JOB_NAME} <${env.BUILD_URL}|#${env.BUILD_NUMBER}>")
                }
            }
        }
        // unstable {}
        failure {
            slackSend (color: '#800000', message: """Build Failed: ${env.JOB_NAME} <${env.BUILD_URL}|#${env.BUILD_NUMBER}>
            Commit SHA: ${env.GIT_COMMIT}""")
        }
        // changed {}
    }
}