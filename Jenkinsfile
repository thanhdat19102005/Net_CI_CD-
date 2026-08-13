pipeline {
    agent any

    environment {
        // Thông tin host và user FTP của MonsterASP
        FTP_HOST = "site84945.siteasp.net"
        FTP_USER = "site84945"
        
        // Fix lỗi thiếu libicu trên Docker Linux
        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "1"
        
        // Đường dẫn để hệ thống nhận diện lệnh dotnet sau khi tải về
        DOTNET_ROOT = "${WORKSPACE}/.dotnet"
        PATH = "${WORKSPACE}/.dotnet:${env.PATH}"
    }

    stages {
        stage('1. Checkout Code') {
            steps {
                // Tự động lấy cấu hình Git từ Job của Jenkins
                checkout scm 
            }
        }

        stage('2. Stop IIS Server (Bảo trì)') {
            steps {
                // Lấy chìa khóa từ két sắt (nhớ đảm bảo bạn đã tạo credential ID là 'ftp-pass')
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
                # Tải và cài đặt .NET 8 SDK trực tiếp từ Microsoft (để tránh lỗi Plugin)
                curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
                bash dotnet-install.sh --channel 8.0 --install-dir ./.dotnet
                
                # Build và Đóng gói code
                dotnet restore
                dotnet build --configuration Release
                dotnet publish --configuration Release --output ./publish_output
                '''
            }
        }

        stage('4. Deploy to MonsterASP') {
            steps {
                // Đẩy toàn bộ ruột thư mục publish_output lên 'MonsterServer' (đã fix lỗi missing parameter)
                ftpPublisher alwaysPublishFromMaster: false, continueOnError: false, failOnError: true, masterNodeName: '', paramPublish: null, publishers: [
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
                // Xóa file bảo trì để web hoạt động lại với code mới
                withCredentials([string(credentialsId: 'ftp-pass', variable: 'FTP_PASS')]) {
                    sh '''
                    curl ftp://${FTP_HOST}/wwwroot/ -X "DELE app_offline.htm" --user ${FTP_USER}:${FTP_PASS}
                    '''
                }
            }
        }
    }
}