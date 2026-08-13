pipeline {
    agent any

    // 1. Kích hoạt .NET 8 bên trong Docker
    tools {
        dotnetsdk 'dotnet-8'
    }

    environment {
        // 2. Thông tin host và user FTP của MonsterASP
        FTP_HOST = "site84945.siteasp.net"
        FTP_USER = "site84945"
    }

    stages {
        stage('1. Checkout Code') {
            steps {
                // Lệnh chuẩn DevOps: Tự động lấy cấu hình Git từ Job của Jenkins
                checkout scm 
            }
        }

        stage('2. Stop IIS Server (Bảo trì)') {
            steps {
                // Lấy chìa khóa từ két sắt
                withCredentials([string(credentialsId: 'ftp-pass', variable: 'FTP_PASS')]) {
                    sh '''
                    echo "Hệ thống đang bảo trì cập nhật code..." > app_offline.htm
                    curl -T app_offline.htm ftp://${FTP_HOST}/wwwroot/ --user ${FTP_USER}:${FTP_PASS}
                    sleep 5
                    '''
                }
            }
        }

        stage('3. Build & Publish .NET 8') {
            steps {
                sh '''
                dotnet restore
                dotnet build --configuration Release
                dotnet publish --configuration Release --output ./publish_output
                '''
            }
        }

        stage('4. Deploy to MonsterASP') {
            steps {
                // Đẩy toàn bộ ruột thư mục publish_output lên 'MonsterServer'
                ftpPublisher alwaysPublishFromMaster: false, continueOnError: false, failOnError: true, publishers: [
                    [configName: 'MonsterServer', 
                     transfers: [
                         [cleanRemote: false, excludes: '', flatten: false, makeEmptyDirs: false, noDefaultExcludes: false, patternSeparator: '[, ]+', remoteDirectory: '', remoteDirectorySDF: false, removePrefix: 'publish_output', sourceFiles: 'publish_output/**']
                     ], 
                     usePromotionTimestamp: false, useWorkspaceInPromotion: false, verbose: true]
                ]
            }
        }

        stage('5. Start IIS Server') {
            steps {
                // Xóa file bảo trì để web hoạt động lại
                withCredentials([string(credentialsId: 'ftp-pass', variable: 'FTP_PASS')]) {
                    sh '''
                    curl ftp://${FTP_HOST}/wwwroot/ -X "DELE app_offline.htm" --user ${FTP_USER}:${FTP_PASS}
                    '''
                }
            }
        }
    }
}


