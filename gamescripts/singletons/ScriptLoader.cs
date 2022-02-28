using Godot;
using Tools;
using godcol = Godot.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;


public struct scriptdata{
  public string language_name;
  public string compiler_name, options;
  public string[] compatible_extensions;
  public string[] dependencies;
}

public class ScriptLoader: Node2D{
  private const string srcFolder = "srccode";
  private const string playerSaveFolder = "players";
  private const string coredll_file = "coredll.dll";
  private const string mainscriptobj = "main.o";
  private const string botlibheader = "botlib.hpp";
  private const string binfolder = "bin";
  private const string dependencyfolder = "dependency";
  private const string JSON_compilerdata = "language_data.json";
  private const string JSON_Folder = "JSONData";
  private string currentUsername = "";
  private ErrorHandler errhand;
  private Autoload autoload_node;
  private Directory directory = new Directory();
  private Dictionary<string, Dictionary<int, int>> programUsages = new Dictionary<string, Dictionary<int, int>>();
  private Dictionary<string, int> programLatestDate = new Dictionary<string, int>();
  private Dictionary<string, scriptdata> ScriptDataDict = new Dictionary<string, scriptdata>();
  private string[] SupportedLanguages = new string[0];
  private File file = new File();


  public delegate void Callback_Compile(programdata pd);

  public struct programdata{
    public string srcname;
    public int date;
  }


  private void CheckAndCopyFile(string from, string to){
    if(directory.FileExists(to)){
      if(file.GetSha256(from) != file.GetSha256(to))
        directory.Copy(from, to);

    }else
      directory.Copy(from, to);
  }

  // this will use the first path as the name of the executables
  private programdata? _CompileManyProgram(string[] paths, scriptdata compdata){
    programdata newpd = new programdata();

    try{
      int i;
      char c;
      for(i = paths[0].Length-1; i >= 0 && (c = paths[0][i]) != '\\' && c != '/'; i--)
        ;
      
      newpd.srcname = paths[0].Remove(0, i+1);
      
      System.IO.FileInfo srcinfo = new System.IO.FileInfo(paths[0]);
      System.DateTime srctime = srcinfo.LastWriteTime;
      newpd.date =
        srctime.Day * 1000000 +
        srctime.Hour * 10000 +
        srctime.Minute * 100 +
        srctime.Second;

      string timestr = newpd.date.ToString();
      string datefilenamepath = paths[0] + "date.dat";
      string outputpath = GetAboslutePathToProg(newpd);
      GD.PrintErr(outputpath);


      // checking if the similar program need to recompile or not
      if(directory.FileExists(outputpath) && directory.FileExists(datefilenamepath)){
        file.Open(datefilenamepath, File.ModeFlags.Read);
        string currentdate = file.GetAsText();

        if(currentdate == timestr){
          GD.PrintErr("Still the same as the previous");
          file.Close();
          return newpd;
        }
      }

      // if the program need to be recompiled again
      file.Open(datefilenamepath, File.ModeFlags.Write);
      file.StoreString(timestr);
      file.Close();

      string[] formattedOption = System.String.Format(compdata.options, outputpath).Split(' ');
      string[] arguments = new string[paths.Length + formattedOption.Length + compdata.dependencies.Length];

      System.Array.Copy(paths, 0, arguments, 0, paths.Length);
      System.Array.Copy(formattedOption, 0, arguments, paths.Length, formattedOption.Length);
      System.Array.Copy(compdata.dependencies, 0, arguments, paths.Length+formattedOption.Length, compdata.dependencies.Length);

      godcol.Array output = new godcol.Array{
        "Cannot compile the specified file(s).\n\n"
      };

      int returnCode = OS.Execute(
        compdata.compiler_name,
        arguments,
        true,
        output,
        true
      );

      //if(!directory.FileExists(outputpath)){
      if(returnCode != 0){
        // this is asychronously called by purpose
  #pragma warning disable
        autoload_node.PromptUserWithAcceptDialog("ErrorMessage", output);
        return null;
      }
    }
    catch(System.Exception e){
      GD.PrintErr(e.ToString());
    }

    programUsages[newpd.srcname] = new Dictionary<int, int>();
    programUsages[newpd.srcname].Add(newpd.date, 0);
    programLatestDate[newpd.srcname] = newpd.date;
    return newpd;
  }

  private void RemovePrograms(){
    string currentDirectory = SavefileLoader.todir(new string[]{
      OS.GetUserDataDir(),
      binfolder
    });

    string[] dirs = System.IO.Directory.GetFiles(currentDirectory, "*." + OS_Data.ExecutableName);
    foreach(string str in dirs)
      System.IO.File.Delete(str);
  }
  

  public override void _Ready(){
    autoload_node = GetNode<Autoload>("/root/Autoload");

    string binfiledir = "user://" + binfolder;
    string dependencyfiledir = "user://" + dependencyfolder;

    errhand = GetNode<ErrorHandler>("/root/ErrorHandler");

    if(!directory.DirExists(dependencyfiledir))
      directory.MakeDir(dependencyfiledir);
    
    if(!directory.DirExists(binfiledir))
      directory.MakeDir(binfiledir);
    else
      RemovePrograms();
    
    
    CheckAndCopyFile("res://LibraryCode_dll//"+coredll_file, "user://"+binfolder+"//"+coredll_file);
    CheckAndCopyFile("res://LibraryCode_dll//mainscript//"+mainscriptobj, "user://"+binfolder+"//"+mainscriptobj);

    string[] users = GetNode<SavefileLoader>("/root/SavefileLoader").getUsers();
    string botheaderdir = "res://LibraryCode_dll//mainscript//"+botlibheader,
      playersfolder = "user://"+playerSaveFolder;
    for(int i = 0; i < users.Length; i++){
      CheckAndCopyFile(botheaderdir, SavefileLoader.todir(new string[]{
        playersfolder,
        users[i],
        srcFolder,
        botlibheader
      }, "//"));

      //GD.PrintErr("user: ", users[i]);
    }

    
    string res_languagedataPath =  SavefileLoader.todir(new string[]{
      "res:/",
      JSON_Folder,
      JSON_compilerdata
    });

    string user_languagedataPath = SavefileLoader.todir(new string[]{
      "user:/",
      JSON_compilerdata
    });

    if(!directory.FileExists(user_languagedataPath))
      directory.Copy(res_languagedataPath, user_languagedataPath);

    //opening the language_data.json
    File f = new File();
    f.Open(user_languagedataPath, File.ModeFlags.Read);
    string jsonstore = f.GetAsText();
    JSONParseResult parsedobj = JSON.Parse(jsonstore);
    GD.Print(parsedobj.ToString());
    if(parsedobj.Result is godcol.Dictionary){
      var dict = (godcol.Dictionary)parsedobj.Result;

      // on every langauge specified
      foreach(string langname in dict.Keys){
        object _subdict = dict[langname];
        if(_subdict is godcol.Dictionary){
          try{
            godcol.Dictionary subdict = (godcol.Dictionary)_subdict;
            string[] compilers = Autoload.JSON_ArrayToStrings((string)subdict["compiler_names"]);
            string currentWorkingCompiler = null;
              
            // checking if the compilers available
            // and taking one of them
            foreach(string str in compilers){
              int exitCode = OS.Execute("where", new string[]{
                "/q",
                str
              });

              // if success
              if(exitCode == 0){
                currentWorkingCompiler = str;
                break;
              }
            }

            if(currentWorkingCompiler == null)
              break;

            string[] filepaths_dependency = new string[0];

            // do check the dependencies for the languages
            // if not specified in the json, it will be forgivable (lang can still be used)
            if(subdict.Contains("dependencies")){
              string[] filenames_dependency = Autoload.JSON_ArrayToStrings((string)subdict["dependencies"]);
              filepaths_dependency = new string[filenames_dependency.Length];

              // checking where the dependency files are
              string OS_UserDir = OS.GetUserDataDir();
              for(int i = 0; i < filenames_dependency.Length; i++){
                string filepath;
                if(directory.FileExists((filepath = SavefileLoader.todir(new string[]{
                  OS_UserDir,
                  dependencyfolder,
                  filenames_dependency[i]
                }))))
                  ;
                
                else if(directory.FileExists((filepath = SavefileLoader.todir(new string[]{
                  OS_UserDir,
                  binfolder,
                  filenames_dependency[i]
                }))))
                  ;

                else{
                  GD.PrintErr("A dependency (", filenames_dependency[i], ") file not found for language (", langname, ").");
                  GD.PrintErr("Language can still be used, but expect some parts that will not work.");
                }

                filepaths_dependency[i] = filepath;
              }
            }
            else
              GD.PrintErr("Dependancy not found for programming language '", langname, "'.");
          
            ScriptDataDict.Add(
              langname,
              new scriptdata{
                language_name = langname,
                compiler_name = currentWorkingCompiler,
                options = (string)subdict["options"],
                compatible_extensions = Autoload.JSON_ArrayToStrings((string)subdict["compatible_extensions"]),
                dependencies = filepaths_dependency
              }          
            );
          }
          catch(System.Exception e){
            GD.PrintErr("Language cannot be used for the game, reasoning:");
            GD.PrintErr(e.Message);
          }
        }
      }

      // if no language that can be used for the game,
      if(ScriptDataDict.Count == 0){

      }

      System.Array.Resize<string>(ref SupportedLanguages, ScriptDataDict.Count);
      int iter = 0;
      foreach(string key in ScriptDataDict.Keys)
        SupportedLanguages[iter++] = key;
    }
  }
  
  // running this function assumes that all the programs are done running
  public void BindUsername(string username){
    if(directory.DirExists(SavefileLoader.todir(new string[]{
      "user:/",
      playerSaveFolder,
      username
    }))){
      currentUsername = username;
    }
    else
      errhand.ErrorLog("Username \""+username+"\" not found!");
  }

  public string GetAboslutePathToProg(programdata progdat){
    return SavefileLoader.todir(new string[]{
      OS.GetUserDataDir(),
      binfolder,
      SavefileLoader.todir(
        new string[]{
          currentUsername,
          progdat.srcname,
          progdat.date.ToString()
        },
        "_"
      ) + "." + OS_Data.ExecutableName
    });
  }

  public void AddUsageOfProgram(programdata progdat){
    programUsages[progdat.srcname][progdat.date] += 1;
  }

  public void RemoveUsageOfProgram(programdata progdat){
    int currentcount = (programUsages[progdat.srcname][progdat.date] -= 1);
    if(currentcount <= 0)
      if(programLatestDate[progdat.srcname] != progdat.date){
        programUsages.Remove(progdat.srcname);
        System.IO.Directory.Delete(GetAboslutePathToProg(progdat));
      }
  }

  public string[] GetSupportedLanguages(){
    return SupportedLanguages;
  }

  // this should ask for the language type first
  // using nullable return type, just in case the user cancels it
  public async Task<programdata?> PickAndCompileScript(){
    string languagetype = (await autoload_node.PickOptionsByPopup(GetSupportedLanguages()));

    if(languagetype == "")
      return null;
    
    scriptdata currentsd = ScriptDataDict[languagetype];
    // get from a data of scripts
    string[] paths = (await autoload_node.GetFilesByFileExplorer(currentsd.compatible_extensions));
    if(paths == null)
      return null;
    
    return _CompileManyProgram(paths, currentsd);
  }

  public async Task PickAndCompileScript(Callback_Compile cc){
    programdata? pd = (await PickAndCompileScript());
    if(pd != null)
      cc((programdata)pd);
  }
}
