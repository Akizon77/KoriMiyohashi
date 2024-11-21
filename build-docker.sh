#!/bin/bash

IMAGE_NAME="kori_miyohashi"
REPO_NAME="akizon77/kori_miyohashi"
TAG="latest"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

# Build Docker image
echo -e "${YELLOW}Building Docker image: ${IMAGE_NAME}...${NC}"
docker build -t $IMAGE_NAME .

# Check if build was successful
if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed, please check the error messages above.${NC}"
    exit 1
fi

echo -e "${GREEN}Build successful!${NC}"

# Tag the image with the repository name
docker tag $IMAGE_NAME $REPO_NAME:$TAG

# Ask if the image should be pushed to the repository
read -p "Do you want to push the image to the repository? (y/n): " push_response

if [[ "$push_response" == "y" || "$push_response" == "Y" ]]; then
    echo -e "${YELLOW}Pushing image to repository: $REPO_NAME with tag $TAG...${NC}"
    docker push $REPO_NAME:$TAG

    # Check if push was successful
    if [ $? -ne 0 ]; then
        echo -e "${RED}Push failed, please check the error messages above.${NC}"
        exit 1
    fi

    echo -e "${GREEN}Image pushed successfully!${NC}"
fi

# Ask if the container should be run
read -p "Do you want to run the container? (y/n): " run_response

if [[ "$run_response" == "y" || "$run_response" == "Y" ]]; then
    echo -e "${YELLOW}Running container...${NC}"
    docker run -it $IMAGE_NAME

    # Check if the container ran successfully
    if [ $? -ne 0 ]; then
        echo -e "${RED}Failed to run the container, please check the error messages above.${NC}"
        exit 1
    fi

    echo -e "${GREEN}Container ran successfully!${NC}"
fi

echo -e "${GREEN}Done!${NC}"
