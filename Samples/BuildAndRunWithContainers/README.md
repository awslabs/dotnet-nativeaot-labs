# Build and/or Run with Containers

This sample's code performs the same function as the [SimpleFunctionWithCustomRuntime](../SimpleFunctionWithCustomRuntime/), however we've also included 2 docker files which can give you the ability to build with and/or run on a container in Lambda.

## Build Only

Since it is likely that your main development machine isn't running Amazon Linux 2 and cross-OS compilation is not supported, you can instead compile your linux-native code on a [Docker](https://www.docker.com/) container running Amazon Linux 2.

To build your Lambda bootstrap file on Amazon Linux 2 inside Docker, run this command inside the same directory as the docker files: 

```DOCKER
docker build -t build-only-image -f DockerfileBuildOnly .
```

(you can change the tag `build-only-image` to whatever you want)

Now to extract the bootstrap file from the container, run the newly created image and then run 

```
docker cp {randomly created container name}:/source/bin/Release/net6.0/linux-x64/native/bootstrap .
```

 (e.g. `docker cp vibrant_chatelet:/source/bin/Release/net6.0/linux-x64/native/bootstrap .`)

After that, you should see a file called bootstrap that is about 20MB in the same directory that you ran the command from.

**Now you can manually zip up the bootstrap file or include it in another docker file to deploy it to Lambda!**

If you want to zip up the file right after building it add the follow 2 lines to the end of `DockerfileBuildOnly`

```DOCKER
RUN cp /source/bin/Release/net6.0/linux-x64/native/bootstrap bootstrap
RUN zip package.zip bootstrap
```

And then to extract the zip instead of the bootstrap file after running the image:

```DOCKER
docker cp {randomly created container name}:/source/package.zip .
```

## Build And Run

Lambda gives you the option to [deploy a container](https://docs.aws.amazon.com/lambda/latest/dg/csharp-image.html) instead of uploading a zip file.

If you don't already have one, you will need to create an [Elastic Container Registry](https://aws.amazon.com/ecr/) repository to upload your container image to.

Once you have one, navigate into it in the AWS Console and you'll see a button called `View publish commands` that will give you all the commands you need to upload your image. You'll only have to change the build command (second command) to specify the docker file to use.

For example, instead of `docker build -t my-test-repository .` instead run `docker build -t my-test-repository -f DockerfileBuildAndRun .`

Once you've followed the 4 commands given from ECR, you can create a new Lambda and point it to the container image you just uploaded.