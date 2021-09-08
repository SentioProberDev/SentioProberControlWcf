# SENTIO® Prober Control - WCF Bindings Sample Client
This archive contain a sample project demonstrating how to connect to the MPI Prober Control Software SENTIO via WCF and C#.

![](https://www.mpi-corporation.com/wp-content/uploads/2019/12/1.-TS3500-SE-with-WaferWallet_frontview.jpg)

# Prerequisites
The Minimum required SENTIO version is 3.5. The SENTIO WCF Server must be active. This is normally the case. In order to check whether the WCF server is active go to the config.xml (C:\ProgramData\MPI Corporation\SENTIO\config\config.xml) and check wether you see this node.

```xml
  <Main>
    ...
    <WcfServer Uri="net.tcp://localhost:35556/Sentio" />
    ...
```
