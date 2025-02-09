CONFIGURATION = Release
PROJECT_DIR = ./src

IMAGE_NAME="kori_miyohashi"
REPO_NAME="akizon77/kori_miyohashi"
TAG="latest"

.PHONY: all build push clean

all: build

build:
	dotnet publish $(PROJECT_DIR) -c $(CONFIGURATION) -p:PublishSingleFile=true --self-contained true -o bin; \

push:
	docker build --no-cache -t $(IMAGE_NAME) .
	docker tag $(IMAGE_NAME) $(REPO_NAME):$(TAG)
	docker push $(REPO_NAME):$(TAG)

clean:
	rm -rf bin