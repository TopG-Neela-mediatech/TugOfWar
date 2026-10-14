using UnityEngine;
public static class TMKOCPlaySchoolConstants
{
    #region Scene Names
    public static string TMKOCPlayMainMenu = "PlayschoolMainScene";
    public static string TMKOCPlayRoomScene = "PlayRoom";
    public static string TMKOCPlayBeachScene = "TMKOCPlayBeachScene";
    #endregion
    #region Game Names
    public static string Coloring = "Coloring";
    public static string MusicalInstruments = "MusicalInstruments";
    public static string Tracing = "Tracing";
    public static string JigsawPuzzles = "JigsawPuzzles";
    public static string FlashCards = "FlashCards";
    public static string BalloonPopping = "BalloonPopping";
    public static string VisualPerceptionGames = "VisualPerceptionGames";
    public static string WordGames = "WordGames";
    public static string CountingGames = "CountingGames";
    public static string Painting = "Painting";
    public static string FindTheLetter = "FindTheLetter";
    public static string MatchTheArrow = "MatchTheArrow";
    public static string CopyThePattern = "CopyThePattern";
    public static string MatchTheShadow = "MatchTheShadow";
    public static string MatchTheColour = "MatchTheColour";
    public static string WorldPersonalities = "WorldPersonalities";
    public static string PeekABoo = "PeekABoo";
    public static string SpotDifference = "SpotDifference";
    public static string FindHiddenObject = "FindHiddenObject";
    #endregion
    #region TokenString
#if PLAYSCHOOL_MAIN
public static string AuthorizationToken = "authorizationToken";
#else
    public static string AuthorizationToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiOTkzMDU5OTA3NCIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IlVzZXIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijc1NTEzM2ZhLTJhYTEtNDM4MS1hZDNkLTUyZjI2ODY1MTVmOCIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjcxNzciLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo3MTc3In0.sw88pwtpfsAWZD-g-aJVAtTGsU964IPEbZTaJL-eGWc";
#endif
    #endregion
    #region Lang Names
    public static string LangName = "LangName";
    public static string Hindi = "Hindi";
    public static string English = "English";
    #endregion
    #region Get Data
    // public static string GetAuthToken = PlayerPrefs.GetString(AuthorizationToken);
    public static int totalStudents = PlayerPrefs.GetInt("totalStudents");
#if PLAYSCHOOL_MAIN
    public static string currentStudentName = "currentStudent";
#else
    public static string currentStudentName = "TestName";
#endif
    
    
    public static string currentStudentPlaying = "currentStudent";
    
    public static string saveDateTime = "saveDateTime";
    public static string LastAttendanceKey = "LastAttendanceDate";
    public static string LastReportCardKey = "LastReportCardKey";
    public static string LastAttendancReportKey = "LastAttendancReprotKey";
    public static string LastProgressReportKey = "LastProgressReportKey";
    #endregion
}
