.PHONY: build client server lint deploy

DOTNET_FLAGS=-c Release -v quiet -maxcpucount:5 /property:WarningLevel=0
DOTNET_BUILD=dotnet build ${DOTNET_FLAGS}

fast: build fastserver fastclient

build:
	${DOTNET_BUILD}

client:
	cd ./bin/Content.Client && ../../linklibs && ./Content.Client

fastclient:
	cd ./bin/Content.Client && ../../linklibs && ./Content.Client --connect-address localhost:1212 --connect && pkill -TERM Content.Server

server:
	cd ./bin/Content.Server && ./Content.Server

fastserver:
	cd ./bin/Content.Server && ./Content.Server &

lint:
	${DOTNET_BUILD} Content.YAMLLinter
	cd bin/Content.YAMLLinter && ../../linklibs && ./Content.YAMLLinter

test:
	cd RobustToolbox/bin/UnitTesting && ../../../linklibs
	cd bin/Content.Tests && ../../linklibs
	dotnet test ${DOTNET_FLAGS}

package:
	python3 Tools/package_server_build.py --hybrid-acz

deploy: package
	mv release/* ~ss14/downloads
