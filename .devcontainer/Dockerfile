ARG VARIANT="bullseye"
FROM buildpack-deps:${VARIANT}-curl

WORKDIR install
COPY dotnet-install.sh install/dotnet-install.sh
RUN cd install && ./dotnet-install.sh --channel 6.0.4xx
RUN cd install && ./dotnet-install.sh --channel 7.0.1xx --quality preview
ENV PATH="${PATH}:/root/.dotnet"

RUN apt-get update -y
RUN apt-get install git zsh zplug libicu-dev -y
COPY .zshrc /root/.zshrc
RUN zsh
RUN chsh -s /bin/zsh