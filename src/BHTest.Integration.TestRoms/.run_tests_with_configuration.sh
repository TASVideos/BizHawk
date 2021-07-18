#!/bin/sh
set -e
cd "$(dirname "$(realpath "$0")")"
home="$PWD/../.."
config="$1"
shift

mkdir -p res
cd res
for j in BullyGB cgb-acid-hell cgb-acid2 dmg-acid2 Gambatte-testroms mealybug-tearoom-tests rtc3test; do
	if [ -e "${j}_artifact" ]; then
		printf "Using existing copy of %s artifact\n" "$j"
	else
		curl -L -o "$j.zip" "https://gitlab.com/tasbot/libre-roms-ci/-/jobs/artifacts/master/download?job=$j"
		unzip "$j.zip" >/dev/null
		rm "$j.zip"
		printf "Downloaded and extracted %s artifact\n" "$j"
	fi
done
#if (which nix >/dev/null 2>&1); then
#	for a in blargg-gb-tests; do
#		printf "(TODO: nix-build %s)\n" "$a"
#	done
#fi
cd ..

export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:$home/output:$home/output/dll"
dotnet test -a "$home/test_output" -c "$config" -l "junit;LogFilePath=$home/test_output/{assembly}.coverage.xml;MethodFormat=Class;FailureBodyFormat=Verbose" -l "console;verbosity=detailed" "$@"
