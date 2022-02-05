using Godot;
using Tools;
using Godot.Collections;
using System.Diagnostics;

public class ScriptLoader: Node2D{
  public struct compilerpath{
    public static string cpp_compiler = "g++";
  }

  private const string srcFolder = "srccode";
  private const string playerSaveFolder = "players";
  private const string coredll_file = "coredll.dll";
  private const string mainscriptobj = "main.o";
  private const string botlibheader = "botlib.hpp";
  private const string binfolder = "bin";
  private string currentUsername = "";
  private ErrorHandler errhand;
  private Directory directory = new Directory();
  private Dictionary<string, Dictionary<int, int>> programUsages = new Dictionary<string, Dictionary<int, int>>();
  private Dictionary<string, int> programLatestDate = new Dictionary<string, int>();
  private File file = new File();

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

  public override void _Ready(){
    string binfiledir = "user://" + binfolder;
    errhand = GetNode<ErrorHandler>("/root/ErrorHandler");
    if(!directory.DirExists(binfiledir))
      directory.MakeDir(binfiledir);

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

      GD.PrintErr("user: ", users[i]);
    }
  }

  // this returns an exepath to the program
  // data will give the s
  public string CompileProgram(string compilerPath, ref programdata data){
    if(currentUsername == ""){
      errhand.ErrorLog("Username isn't specified.");
      return "";
    }

    if(data.srcname == ""){
      errhand.ErrorLog("Src name isn't specified.");
      return "";
    }

    string srcpath = SavefileLoader.todir(new string[]{
      OS.GetUserDataDir(),
      playerSaveFolder,
      currentUsername,
      srcFolder
    });

    System.IO.FileInfo srcinfo = new System.IO.FileInfo(srcpath);
    System.DateTime srctime = srcinfo.LastWriteTime;
    data.date =
      srctime.Day * 1000000 +
      srctime.Hour * 10000 +
      srctime.Minute * 100 +
      srctime.Second;

    string timestr = data.date.ToString();

    string datefilename = data.srcname + "date.dat";
    string datefilenamepath = SavefileLoader.todir(new string[]{
      srcpath,
      datefilename
    });

    string outputpath = GetAboslutePathToProg(data);

    if(directory.FileExists(outputpath) && directory.FileExists(datefilenamepath)){
      file.Open(datefilenamepath, File.ModeFlags.Read);
      string currentdate = file.GetAsText();
      GD.Print(currentdate);
      if(currentdate == timestr){
        GD.PrintErr("Still the same as the previous");
        file.Close();
        return outputpath;
      }
    }

    file.Open(datefilenamepath, File.ModeFlags.Write);
    file.StoreString(timestr);
    file.Close();

    string[] arguments = new string[]{
      SavefileLoader.todir(new string[]{
        srcpath,
        data.srcname
      }),
    
      SavefileLoader.todir(new string[]{
        OS.GetUserDataDir(),
        binfolder,
        coredll_file
      }),
      
      SavefileLoader.todir(new string[]{
        OS.GetUserDataDir(),
        binfolder,
        mainscriptobj
      }),

      "-o",
      outputpath
    };

    Array output = new Array();
    OS.Execute(
      path: compilerPath,
      arguments: arguments,
      
      true,
      output
    );

    if(!directory.FileExists(outputpath)){
      GD.PrintErr("Can't compile a program,\n");
      for(int i = 0; i < output.Count; i++)
        GD.PrintErr((string)output[i]);

      return "";
    }else{
      programUsages[data.srcname] = new Dictionary<int, int>();
      programLatestDate[data.srcname] = data.date;
    }

    return outputpath;
  }
  
  //running this function assumes that all the programs are done running
  public void BindUsername(string username){
    if(directory.DirExists(SavefileLoader.todir(new string[]{
      "user:/",
      playerSaveFolder,
      username
    }))){
      ClearUnnecessaryPrograms();
      currentUsername = username;
      GetLatestDateInPrograms();
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
      ) + ".exe"
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

  // getting datas from date.dat files
  public void GetLatestDateInPrograms(){
	
  }
}
