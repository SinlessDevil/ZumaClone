FROM ubuntu:22.04

RUN apt-get update && apt-get install -y \
    wget unzip git curl \
    libgtk-3-0 libnss3 libxss1 libasound2 libxrandr2 libglu1-mesa \
    openjdk-17-jdk \
    apt-transport-https software-properties-common python3-pip \
    && rm -rf /var/lib/apt/lists/*

RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --version 7.0.100 --install-dir /usr/share/dotnet

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:/usr/share/dotnet"

RUN dotnet tool install --global Cake.Tool --version 1.3.0
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN pip install gdown

WORKDIR /opt/unity
ENV UNITY_TGZ_ID=1d6Pqc6y92yACE_mhctF3NkhiUkX_iQzv
RUN gdown https://drive.google.com/uc?id=${UNITY_TGZ_ID} -O Unity.tar.xz

RUN mkdir unity && \
    tar -xf Unity.tar.xz -C unity && \
    mv unity/Editor Unity && \
    rm -rf Unity.tar.xz unity

ENV UNITY_PATH=/opt/unity/Unity/Editor/Unity
ENV PATH="${PATH}:/opt/unity/Unity/Editor"

WORKDIR /project
