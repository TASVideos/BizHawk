#!/bin/sh
cd "$(dirname "$(realpath "$0")")" && ./.run_tests_with_configuration.sh "Debug" "$@"
