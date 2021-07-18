#!/bin/sh
compare -metric AE "$1" "$2" NULL: 2>&1 >/dev/null
