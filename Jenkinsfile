pipeline {
    agent any

    // ĐÃ XÓA KHỐI tools {} CỦA JENKINS VÌ PLUGIN ĐANG BỊ LỖI

    environment {
        FTP_HOST = "site84945.siteasp.net"
        FTP_USER = "site84945"
        
        // Thêm đường dẫn để hệ thống nhận diện được lệnh dotnet sau khi tải về
        DOTNET_ROOT = "${WORKSPACE}/.dotnet"
        PATH = "${WORKSPACE}/.dotnet:${env.PATH}"
    }

    stages {
        stage('1. Checkout Code') {
            steps {
                checkout scm 
            }
        }

        stage('2. Stop IIS Server (Bảo trì)') {
            steps {
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
                # 1. Tải script cài đặt trực tiếp từ Microsoft
                curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
                
                # 2. Cài đặt .NET 8 vào thư mục .dotnet ẩn trong project
                bash dotnet-install.sh --channel 8.0 --install-dir ./.dotnet
                
                # 3. Tiến hành Build code như bình thường
                dotnet restore
                dotnet build --configuration Release
                dotnet publish --configuration Release --output ./publish_output
                '''
            }
        }

        stage('4. Deploy to MonsterASP') {
            steps {
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
                withCredentials([string(credentialsId: 'ftp-pass', variable: 'FTP_PASS')]) {
                    sh '''
                    curl ftp://${FTP_HOST}/wwwroot/ -X "DELE app_offline.htm" --user ${FTP_USER}:${FTP_PASS}
                    '''
                }
            }
        }
    }
}