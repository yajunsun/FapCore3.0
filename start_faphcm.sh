#cd fapcore
cd /var/lib/jenkins-home/workspace/fapcore30
#stop web container [fapcorehcm]
sudo docker stop fapcorehcm
#remove web container [fapcorehcm]
sudo docker rm fapcorehcm
#remove web image [fapcore/hcm]
sudo docker rmi fapcore/hcm 
#build web image [fapcore/hcm]
sudo docker build -t fapcore/hcm .
#docker run container,���ݾ����£�-v ʱ��,-v logs,-v ������˵��������nlog.config��־·��Ϊ��/var/fapcore/logs�������ø���·��Ϊ��/var/fapcore/annex��
sudo docker run --name fapcorehcm -d -p 80:80 -p 443:443 -v /etc/localtime:/etc/localtime -v /usr/docker/fapcorehcm/logs:/var/fapcore/logs -v /usr/docker/fapcorehcm/annex:/var/fapcore/annex fapcore/hcm