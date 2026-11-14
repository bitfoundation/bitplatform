# The image `bit ci` runs ci.yml in on this machine, with act (https://nektosact.com): act's runner image, plus the JDK 17
# and the Android SDK command-line tools that GitHub's hosted runner comes with and act's image lacks, which
# `bit setup --platforms android` builds the Android app with. bit ci builds it on first use and whenever this file changes,
# for linux/amd64, at the Ubuntu version of the job's runs-on, e.g. 24.04 for ubuntu-24.04.
ARG UBUNTU_VERSION=26.04
FROM ghcr.io/catthehacker/ubuntu:pwsh-${UBUNTU_VERSION}

ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64 \
    ANDROID_HOME=/usr/local/lib/android/sdk \
    ANDROID_SDK_ROOT=/usr/local/lib/android/sdk

RUN apt-get update -qq \
 && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends openjdk-17-jdk-headless unzip \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p "$ANDROID_HOME/cmdline-tools" \
 && curl -fsSL -o /tmp/cmdline-tools.zip https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip \
 && unzip -q /tmp/cmdline-tools.zip -d "$ANDROID_HOME/cmdline-tools" \
 && mv "$ANDROID_HOME/cmdline-tools/cmdline-tools" "$ANDROID_HOME/cmdline-tools/latest" \
 && rm /tmp/cmdline-tools.zip \
 && (yes | "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" --licenses > /dev/null || true) \
 && "$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" platform-tools > /dev/null \
 && java -version
