# Installation

***
***Disclaimer***
This instruction is written on 30.06.2021 for Tune application version 3.4 and the current (at that moment) version of iAPI and OpenMS. It might require some changes if later versions of the software are used.
***

## Thermo instrument API (iAPI)

Instrument API is necessary for the software to work, you will need to obtain the iAPI libraries to build the software and license the iAPI to run the software on your instrument

The complete procedure is described in the [Tune 3.4 Update Overview](http://www.planetorbitrap.com/uploads/PlanetOrbitrapA2463.pdf), slides 51 - 58.

Briefly it includes the following
  1.  Downloaded the iAPI libraries from [Thermo iAPI repository](https://github.com/thermofisherlsms/iapi)
  2.  Log into Almanac agent on the instrument computer
  3.  Open **About Tune** menu in Tune software: Settings -> About Tune
  4.  In a dropdown menu of **License** button select **Get API License**
  5.  Read and acept license agreement and then generate E-Token
  6.  The generated E-Token will be sent to the e-mail address you used to register Almanac agent
  7.  Copy E-Token to the activation form and once the license is received activate it.

## Building FlashIDA tool

This repository contains Visual Studio (version 2019) project that can be used to build the software.

Project targets .NET 4.8, if you have Tune 3.4 installed, you should have the required version of .NET already.

Please, note, that you will need to copy the following DLLs into `dependecies` folder to build the software sucessfully.

From Thermo iAPI

 * `API-2.0.dll`
 
 * `Fusion.API-1.0.dll`
 
 * `Spectrum-1.0.dll`
 
 * `Thermo.TNG.Factory.dll`
 
From existing Tune application installation

 * `Thermo.TNG.Client.API.dll`, should be located in `C:\Thermo\Instruments\TNG\[NameOfYourInstrument]\[TuneVersion]\System\Programs`

## Setting up FlashIDA

FlashIDA does not require specific installation and can run from any valid location. It requires `FlashDeconv` version of `OpenMS.dll` library, that is distributed with the software, as well as a few other libraries from the `OpenMS` project, minimal set is described below, while complete OpenMS installation can be used as well. Please, make sure that the running folder is writable, since FlashIDA writes log files to the working folder.

* Install, if it is not already installed `Microsoft Visual C++ 2015-2019 Redistributable (x64)` on the instrument computer, it can be [downloaded from Microsoft](https://aka.ms/vs/16/release/vc_redist.x64.exe)
* Copy the following files from OpenMS `bin` folder to the folder with the software
    + `OpenMS_GUI.dll`
    + `OpenSwathAlgo.dll`
    + `Qt5Core.dll`
    + `Qt5Network.dll`
    + `SuperHirn.dll`
* Copy `share\OpenMS` folder from OpenMS to the folder with the software, but keep the hirearchy, i.e. the software folder should contain `share` that contains `OpenMS` folder with all subfolders
* Set the enironment variable `OPENMS_DATA_PATH` to the location of `OpenMS` folder that you have copied at the previous step, i.e. if you place the softwatre to `C:\FlashIDA`, the value of the variable should be `C:\FlashIDA\share\OpenMS`. It should be possible to use existing OpenMS installation as well

Software folder should look similar to this (file-level information is shown only at the first level
```
│   API-2.0.dll
│   Flash.exe
│   Flash.exe.config
│   Fusion.API-1.0.dll
│   log4net.dll
│   method.xml
│   Mono.Options.dll
│   OpenMS.dll
│   OpenMS_GUI.dll
│   OpenSwathAlgo.dll
│   Qt5Core.dll
│   Qt5Network.dll
│   Spectrum-1.0.dll
│   SuperHirn.dll
│   System.Threading.Tasks.Dataflow.dll
│   Thermo.TNG.Factory.dll
│   
└───share
    └───OpenMS
        ├───CHEMISTRY
        │       
        ├───CV
        │       
        ├───DESKTOP
        │       
        ├───examples
        │   │   
        │   ├───BSA
        │   │       
        │   ├───CHROMATOGRAMS
        │   │       
        │   ├───external_code
        │   │       
        │   ├───FRACTIONS
        │   │       
        │   ├───ID
        │   │       
        │   ├───QCImporter
        │   │       
        │   ├───simulation
        │   │       
        │   └───TOPPAS
        │       │   
        │       └───data
        │           ├───BSA_Identification
        │           │       
        │           ├───Identification
        │           │       
        │           └───merger_tutorial
        │                   
        ├───GUISTYLE
        │       
        ├───IDPool
        │       
        ├───MAPPING
        │       
        ├───PIP
        │       
        ├───SCHEMAS
        │       
        ├───SCRIPTS
        │       
        ├───THIRDPARTY
        │       
        ├───TOOLS
        │   └───EXTERNAL
        │       │   
        │       ├───LINUX
        │       │       
        │       └───WINDOWS
        │               
        └───XSL
                
```

To check that the software is working try running it without any parameters, you should be able to see output similar to this
```
INFO - Event: Initializing Remote Client for FusionInstrumentAccessContainer
INFO - Load default CAL file and Tune file.
INFO - Load current CAL file.
INFO - Event: FusionInstrumentAccessContainer Created
INFO - Event: Connected to Manager
INFO - Event: Online access started
INFO - Event: FusionInstrumentAccess Created
    3382 [INFO ] - Instrument Orbitrap Eclipse with ETD  PTR   (1) is connected
INFO - Event: FusionControl Created
INFO - Event: Acquisition Created
    3400 [INFO ] - Switching instrument on...
    3403 [INFO ] - Number of MS: 1
INFO - Event: FusionMsScanContainer Created
INFO - Event: FusionScans Created
    3609 [INFO ] - ScanControl success
    3633 [INFO ] - Read method
    3634 [INFO ] - Created default and AGC scans
    3634 [INFO ] - ScanScheduler created
 FLASHIda creating ... 
    4732 [INFO ] - Instrument Status: On
QScore threshold: 0.25
"tol" -> "[10.0, 10.0]" (ppm tolerance for MS1, 2, ... (e.g., -tol 10.0 5.0 to specify 10.0 and 5.0 ppm for MS1 and MS2, respectively))
"min_mass" -> "500.0" (minimum mass (Da))
"max_mass" -> "1.0e05" (maximum mass (Da))
"min_charge" -> "4" (minimum charge state for MS1 spectra (can be negative for negative mode))
"max_charge" -> "50" (maximum charge state for MS1 spectra (can be negative for negative mode))
"min_mz" -> "-1.0" (if set to positive value, minimum m/z to deconvolute.)
"max_mz" -> "-1.0" (if set to positive value, maximum m/z to deconvolute.)
"min_rt" -> "-1.0" (if set to positive value, minimum RT to deconvolute.)
"max_rt" -> "-1.0" (if set to positive value, maximum RT to deconvolute.)
"min_isotope_cosine" -> "[0.8, 0.9]" (cosine threshold between avg. and observed isotope pattern for MS1, 2, ... (e.g., -min_isotope_cosine_ 0.8 0.6 to specify 0.8 and 0.6 for MS1 and MS2, respectively))
"min_qscore" -> "0.0" (minimum QScore threshold. QScore is the probability that a mass is identified, learned by a logistic regression.)
"min_peaks" -> "[2, 1]" (minimum number of supporting peaks for MS1, 2, ...  (e.g., -min_peaks 3 2 to specify 3 and 2 for MS1 and MS2, respectively))
"max_mass_count" -> "[-1, -1]" (maximum mass count per spec for MS1, 2, ... (e.g., -max_mass_count_ 100 50 to specify 100 and 50 for MS1 and MS2, respectively. -1 specifies unlimited))
"min_mass_count" -> "[-1, -1]" (minimum mass count per spec for MS1, 2, ... this parameter is only for real time acquisition. the parameter may not be satisfied in case spectrum quality is too poor. (e.g., -max_mass_count_ -1 2 to specify no min limit and 2 for MS1 and MS2, respectively. -1 specifies unlimited))
"min_intensity" -> "0.0" (intensity threshold)
"rt_window" -> "180.0" (RT window for MS1 deconvolution)

    6950 [INFO ] - Created FLASHIDA processor
    7043 [INFO ] - Created DataPipe
    7045 [INFO ] - Waiting for contact closure
```
