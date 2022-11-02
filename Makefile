all: clean restore build publish

clean:
	dotnet clean

build:
	dotnet build

restore:
	dotnet restore

publish:
	mkdir -p bin
	dotnet publish -c Release --sc --use-current-runtime -p:PublishSingleFile=true -o ./bin
