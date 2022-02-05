using Godot;
using System;

/* This code haven't tested yet */


public class SavefileLoader: Node2D{
  private const string savefolder = "players";
  private const string srcFolder = "srccode";
  private const string currentsaveDir = "user://" + savefolder;
  
  private Directory directory = new Directory();
  
  
  public struct SaveData{

  }

  public static string todir(string[] files, string conjuntion = "/"){
    string res = "";
    if(files.Length > 0){
      res = files[0];
      for(int i = 1; i < files.Length; i++)
        res += conjuntion + files[i];
    }

    return res;
  }

  public override void _Ready(){
    if(!directory.DirExists(currentsaveDir))
      directory.MakeDir(currentsaveDir);
      
  }

  public SaveData getUserSave(string username){
    SaveData res = new SaveData();

    return res;
  }

  public string[] getUsers(){
    string usersfolder = OS.GetUserDataDir() + "/" + savefolder;
    string[] users = System.IO.Directory.GetDirectories(OS.GetUserDataDir() + "/" + savefolder);
    for(int i = 0; i < users.Length; i++)
      users[i] = users[i].Remove(0, usersfolder.Length+1);
    
    return users;
  }

  public void addUser(string username){
    string userdir = todir(new string[]{
      currentsaveDir,
      username
    });

    if(!directory.DirExists(userdir)){
      directory.MakeDir(userdir);
      directory.MakeDir(todir(new string[]{
        userdir,
        srcFolder
      }));
    }
  }
  
  public bool isUserExists(string username){
    return directory.DirExists(currentsaveDir + username);
  }
}
