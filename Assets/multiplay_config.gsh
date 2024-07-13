version: 1.0
builds:
  Linux Remote Build: # replace with the name for your build
    executableName: linux-build.x86_64 # the name of your build executable
    buildPath: linuxserverv10 # the location of the build files
buildConfigurations:
  Linux Remote Build Config: # replace with the name for your build configuration
    build: Linux Remote Build # replace with the name for your build
    queryType: sqp # sqp or a2s, delete if you do not have logs to query
    binaryPath: linux-build.x86_64  # the name of your build executable
    commandLine: -nographics -batchmode -port $$port$$ -queryport $$query_port$$ -log $$log_dir$$/Engine.log # launch parameters for your server
    variables: {}
    cores: 1 # number of cores per server
    speedMhz: 750 # launch parameters for your server
    memoryMiB: 800 # launch parameters for your server
fleets:
  Fleet 1: # replace with the name for your fleet
    buildConfigurations:
      - Linux Remote Build Config # replace with the names of your build configuration
    regions:
      Europe: # North America, Europe, Asia, South America, Australia
        minAvailable: 0 # minimum number of servers running in the region
        maxServers: 1 # maximum number of servers running in the region
