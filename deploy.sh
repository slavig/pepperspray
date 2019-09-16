echo "DEPLOYING"; 
scp pepperspray/bin/Release/pepperspray.{exe,pdb} srv:~/pepperspray/patch; 
scp -r pepperspray/bin/Release/ru srv:~/pepperspray
scp -r ../peppersprayWeb/peppersprayWeb/dist/* srv:~/www
