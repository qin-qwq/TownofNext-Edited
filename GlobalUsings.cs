
// For global using
// They will be used by default throughout the project
// it is recommended to add only those that are used most often

global using HarmonyLib;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reflection;

// Credit: xtracube

#if ANDROID
global using AmongUsClient_CoJoinOnlinePublicGame = AmongUsClient._CoJoinOnlinePublicGame_d__50;
global using AmongUsClient_DisplayClassToken = AmongUsClient.__c__DisplayClass50_0;
global using IntroCutscene_CoBegin = IntroCutscene._CoBegin_d__34;
global using IntroCutscene_ShowRole = IntroCutscene._ShowRole_d__40;
#else
global using AmongUsClient_CoJoinOnlinePublicGame = AmongUsClient._CoJoinOnlinePublicGame_d__49;
global using AmongUsClient_DisplayClassToken = AmongUsClient.__c__DisplayClass49_0;
global using IntroCutscene_CoBegin = IntroCutscene._CoBegin_d__35;
global using IntroCutscene_ShowRole = IntroCutscene._ShowRole_d__41;
#endif
