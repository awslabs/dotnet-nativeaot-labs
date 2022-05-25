# Build and/or Run with Containers

This sample's code performs the same function as the [SimpleFunctionWithCustomRuntime](../SimpleFunctionWithCustomRuntime/README.md), however we've also included 2 docker files. The first allows you to build on an AL2 docker container, preventing the need to spin up a whole AL2 VM. The second will both build your native code and also run it hosted in a container in Lambda. Before using the images below, you will need to [Authenticate your Docker client to the Amazon Linux Public registry](https://docs.aws.amazon.com/AmazonECR/latest/userguide/amazon_linux_container_image.html). You can also get the login command by navigating to your own AWS ECR repository and then clicking 'View push commands'.

## Build Only

Since it is likely that your main development machine isn't running Amazon Linux 2 and cross-OS compilation is not supported, you can instead compile your linux-native code on a [Docker](https://www.docker.com/) container running Amazon Linux 2.

To build your Lambda bootstrap file on Amazon Linux 2 inside Docker, run the below command inside the directory that contains your csproj and the docker file. Make sure you've set AssemblyName to 'bootstrap' since that is what the docker file is looking for: 

```BASH
docker build -t build-only-image -f DockerfileBuildOnly .
```

(you can change the tag `build-only-image` to whatever you want)

Now to extract the zipped up bootstrap file from the container, run the newly created image in Docker and then run this command

```BASH
docker cp {randomly created container name}:/source/package.zip .
```

 (e.g. `docker cp vibrant_chatelet:/source/package.zip .`)

After that, you should see a file called package.zip in the same directory that you ran the command from.

## Build And Run

Lambda gives you the option to [deploy a container](https://docs.aws.amazon.com/lambda/latest/dg/csharp-image.html) instead of uploading a zip file.

If you don't already have one, you will need to create an [Elastic Container Registry](https://aws.amazon.com/ecr/) repository to upload your container image to.

Once you have one, navigate into it in the AWS Console and you'll see a button called `View publish commands` that will give you all the commands you need to upload your image. You'll only have to change the build command (second command) to specify the docker file to use.

For example, instead of `docker build -t my-test-repository .` instead run `docker build -t my-test-repository -f DockerfileBuildAndRun .`

Once you've followed the 4 commands given from ECR, you can create a new Lambda and point it to the container image you just uploaded.
