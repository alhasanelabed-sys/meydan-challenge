#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../android"
gradle :app:assembleRelease
mkdir -p ../artifacts
unsigned=app/build/outputs/apk/release/app-release-unsigned.apk
build_tools="${ANDROID_SDK_ROOT:-${ANDROID_HOME:?Set ANDROID_HOME}}/build-tools/35.0.0"
"$build_tools/zipalign" -f -p 4 "$unsigned" ../artifacts/aligned.apk
if [ -z "${MEYDAN_KEYSTORE:-}" ]; then
 signing_directory=../artifacts/.signing-private
 mkdir -p "$signing_directory"
 chmod 700 "$signing_directory"
 MEYDAN_KEYSTORE="$signing_directory/preview.keystore"
 if [ ! -f "$signing_directory/password" ]; then
  umask 077
  python3 -c 'import secrets; print(secrets.token_urlsafe(32))' > "$signing_directory/password"
 fi
 export MEYDAN_STORE_PASSWORD="$(cat "$signing_directory/password")"
 if [ ! -f "$MEYDAN_KEYSTORE" ]; then
  keytool -genkeypair -keystore "$MEYDAN_KEYSTORE" -storepass:env MEYDAN_STORE_PASSWORD -keypass:env MEYDAN_STORE_PASSWORD -alias meydan -keyalg RSA -keysize 3072 -validity 3650 -dname 'CN=Meydan Challenge, OU=Preview, O=Meydan' -noprompt
 fi
fi
"$build_tools/apksigner" sign --ks "$MEYDAN_KEYSTORE" --ks-key-alias meydan --ks-pass env:MEYDAN_STORE_PASSWORD --key-pass env:MEYDAN_STORE_PASSWORD --out ../artifacts/meydan-android.apk ../artifacts/aligned.apk
"$build_tools/apksigner" verify --verbose ../artifacts/meydan-android.apk
rm ../artifacts/aligned.apk
