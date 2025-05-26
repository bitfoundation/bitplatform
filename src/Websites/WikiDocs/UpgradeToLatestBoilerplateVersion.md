**Upgrading BitPlatform Boilerplate from Version X to Y**

Upgrading the BitPlatform boilerplate from one version (e.g., 9.7.3) to another (e.g., 9.7.4) can be challenging, but it’s worth the effort due to the numerous new features and improvements added to your project.
To avoid issues, we recommend not letting too much time pass between upgrades, as frequent updates reduce the complexity of merging changes.
We’ve worked to simplify upgrades by moving much of the code into reusable packages like `Bit.BlazorUI`, `Bit.Bswup`, `Bit.Butil`, and `Bit.Besql`, which streamline integration and reduce conflicts.
The following process should **not** be performed in your actual project folder to avoid accidental overwrites or data loss. Instead, perform these steps in a separate temporary directory.
The provided code is tailored for the **Windows Command Line**, but you can share it with an AI chatbot to request equivalents for other shells, such as Bash for Linux/macOS.
You can also modify the boilerplate versions (e.g., 9.7.3 to 9.7.4) or `dotnet new` parameters (e.g., `--signalR`) to match your project’s requirements.

1. **Run the Test Upgrade in a Temporary Directory**
   - Create a new temporary directory (e.g., `C:\Temp\BoilerplateUpgradeTest`) and run the following commands in the Windows Command Line to simulate the upgrade:

     ```cmd
     mkdir BoilerplateUpgradeTest
     cd BoilerplateUpgradeTest
     dotnet new install Bit.Boilerplate::9.7.3
     dotnet new bit-bp --name UpgradeTestProject --signalR
     cd UpgradeTestProject
     git init
     git add .
     git commit -m "project with older version"
     for /d %i in (*) do rd /s /q "%i"
     for /f "delims=" %i in ('dir /b /a-d ^| findstr /v ".git"') do del /f /q "%i"
     cd ..
     dotnet new install Bit.Boilerplate::9.7.4
     dotnet new bit-bp --name UpgradeTestProject --signalR --force
     cd UpgradeTestProject
     git add .
     ```

2. **Compare Old and New Boilerplate Versions**
   - Use your preferred Git client (e.g., VS Code, Visual Studio, GitHub Desktop) or a diff tool to compare the new boilerplate (e.g., 9.7.4) with the old one (e.g., 9.7.3) and identify changes in `UpgradeTestProject` folder.

3. **Port Changes to Your Main Project**
   - Manually merge changes from the new boilerplate into your main project, leveraging packages like `Bit.BlazorUI`, `Bit.Bswup`, `Bit.Butil`, and `Bit.Besql` to adopt new functionality while preserving your custom developments.

By performing the upgrade in a separate directory and carefully merging changes, you can safely update your project.
While the process may take time, the new features and optimizations, combined with the use of streamlined packages, will significantly enhance your project. Frequent upgrades help minimize the effort required.