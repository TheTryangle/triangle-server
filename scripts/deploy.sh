# Create publish artifact
dotnet publish -c Release src

printf 'Zipping files'
#Zip the files
tar -zcvf package.tgz src/bin/Release/netcoreapp1.0/publish/
printf 'done zipping files'
printf 'sending file to deploy'
export SSHPASS=$DEPLOY_PASS
sshpass -e scp package.tgz $DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PATH
printf 'file sent'

printf 'deploying with SSH'
eval 'sshpass -e ssh "$DEPLOY_USER"@"$DEPLOY_HOST" << EOF
cd `dirname "$DEPLOY_PATH"`
tar -xzf package.tgz build
rm package.tgz
rm -rf www_old
mv "$DEPLOY_PATH" www_old
mv build "$DEPLOY_PATH"
EOF'
printf 'done deploying'