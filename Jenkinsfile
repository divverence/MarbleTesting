@Library('JenkinsHelper')
import divv.JenkinsHelper
def helper = new JenkinsHelper(this)

pipeline 
{
    agent any
    environment {
        BUILDTRIGGERD = 'false'
    }
    triggers 
    {
        cron('0 1 * * *')
    }
    stages 
    {
        stage('Prune local only tags') 
        {
            steps 
            {
                bat 'git fetch --prune origin "+refs/tags/*:refs/tags/*"'
            }
        }
        stage('Fetch master and develop refs from origin') 
        {
            steps 
            {
                script
                {
                    // Because DashDashVersion requires them to be up to date to calculate a accurate version number
                    bat 'git fetch origin master develop'
                }
            }
        }
        stage('Submodules') 
        {
            steps 
            {
                // ToDo: if submodule out-of-date, delete build\packages dir!
                bat 'git submodule update --init'
                // Force Develop, master to exist for gitversion to be happy - each command will fail (by design) if current branch IS that branch, hence the exit 0
            }
        }
        stage('Build') 
        {
            when {not{triggeredBy 'TimerTrigger'} }
            steps 
            {
                script
                {
                    BUILDTRIGGERD ='true'
                    bat 'build/MyGet.bat'
                }
            }
        }
        stage('Update dependencies')
        {
            when {allOf{triggeredBy 'TimerTrigger'; branch 'develop'}}
            steps 
            {
               script 
                {                 
                    try
                    {
                        bat 'build/UpdateDependencies.bat'
                    }
                    finally 
                    {
                        helper.OnUpgradeFinish(currentBuild.rawBuild.log.toString())
                    }
                }
                echo currentBuild.result
            }
        }
    }
    post 
    {
        always {
            script
            {
                if(BUILDTRIGGERD == 'true')
                {
                    step([
                        $class: 'MSTestPublisher',
                        testResultsFile: '**/testresults*.xml',
                        failOnError: false,
                        keepLongStdio: true])
                    step([
                        $class: 'CoberturaPublisher',
                        autoUpdateHealth: false,
                        autoUpdateStability: false,
                        coberturaReportFile: 'built/coverage.cobertura.xml',
                        failUnhealthy: false,
                        failUnstable: false,
                        maxNumberOfBuilds: 0,
                        onlyStable: false,
                        sourceEncoding: 'ASCII',
                        zoomCoverageChart: false])
                }
            }
        }
        success {
            script 
            {
                if(BUILDTRIGGERD == 'true')
                {
                    helper.OnSucces()
                }
            }
        }
        failure {
            script 
            {
                if(BUILDTRIGGERD == 'true')
                {
                    helper.OnFail()
                }
            }
        }
        unstable {
            script 
            {
                if(BUILDTRIGGERD == 'true')
                {
                    helper.OnUnstable()
                }
            }
        }
    }
}