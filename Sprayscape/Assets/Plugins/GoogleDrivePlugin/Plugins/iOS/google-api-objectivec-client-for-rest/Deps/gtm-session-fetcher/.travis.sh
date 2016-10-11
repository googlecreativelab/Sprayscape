#!/usr/bin/env bash

set -eu

if [[ "$#" -ne 2 ]]; then
  echo "Usage: $0 {iOS|OSX} {Debug|Release}"
  exit 10
fi

BUILD_MODE="$1"
BUILD_CFG="$2"

CMD_BUILDER=(
  xcodebuild \
    -project Source/GTMSessionFetcherCore.xcodeproj
)

case "${BUILD_MODE}" in
  iOS)
    CMD_BUILDER+=(
        -scheme "iOS Framework"
        -destination "platform=iOS Simulator,name=iPhone 6,OS=latest"
    )
    ;;
  OSX)
    CMD_BUILDER+=(-scheme "OS X Framework")
    ;;
  *)
    echo "Unknown BUILD_MODE: ${BUILD_MODE}"
    exit 11
    ;;
esac

case "${BUILD_CFG}" in
  Debug|Release)
    CMD_BUILDER+=(-configuration "${BUILD_CFG}")
    ;;
  *)
    echo "Unknown BUILD_CFG: ${BUILD_CFG}"
    exit 12
    ;;
esac

CMD_BUILDER+=(
  build test
)

set -x
exec "${CMD_BUILDER[@]}"
