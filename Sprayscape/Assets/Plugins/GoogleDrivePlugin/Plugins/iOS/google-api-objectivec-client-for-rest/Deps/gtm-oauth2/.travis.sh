#!/usr/bin/env bash

set -eu

if [[ "$#" -ne 2 ]]; then
  echo "Usage: $0 {iOS|OSX} {Debug|Release}"
  exit 10
fi

BUILD_MODE="$1"
BUILD_CFG="$2"

CMD_BUILDER=(
  xcodebuild
)

case "${BUILD_MODE}" in
  iOS)
    CMD_BUILDER+=(
        -workspace Examples/OAuth2SampleTouch/OAuth2SampleTouch.xcworkspace
        -scheme OAuthSampleTouch
        -sdk iphonesimulator
        # No -destination since there are no tests.
    )
    ;;
  OSX)
    CMD_BUILDER+=(
        -workspace Examples/OAuth2Sample/OAuth2Sample.xcworkspace
        -scheme OAuth2Sample
        # No -destination since there are no tests.
    )
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
  build
)

set -x
exec "${CMD_BUILDER[@]}"
