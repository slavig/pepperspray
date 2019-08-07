response to joinroom:
	S `joinedroom=BDSM_club=`

New player
```
SERV<< newplayer=AAAA=f=
```

Chardata ex
```
MSGA>> private AAAA ~action/givemeCharData|BBBB
MSG<< 098890dde069e9abad63f19a0d9e1f32 Game_Server ~action/givemeCharData|AAAA
MSGA>> private AAAA ~action/charData|BBBB|==b64==|
MSG<< 098890dde069e9abad63f19a0d9e1f32 Game_Server ~action/charData|AAAA|==b64==|
```

Position ex
```
MSGA>> private AAAA ~action/getPosition|BBBB|f
MSG<< 098890dde069e9abad63f19a0d9e1f32 Game_Server ~action/getPosition|AAAA|f
MSGA>> private AAAA ~action/SetUserVar|BBBB|vaginal_sex|no|
...
MSGA>> private ,AAAA,~action/send_position_complete|BBBB
MSG<< 098890dde069e9abad63f19a0d9e1f32 Game_Server ~action/SetUserVar|AAAA|vaginal_sex|no|
...
MSG<< 098890dde069e9abad63f19a0d9e1f32 Game_Server ~action/send_position_complete|AAAA
```

Runchat
```~ask/runchat|Player1465439686|True|house|```` from **Player486093847**
Starts lobby **"Player486093847_house**


Open/close room
```
<= openroom  Ezekiel_2517_room house 0 ForAll
<= closeroom Ezekiel_2517_room
```

Public rooms
```
userroomlist=xXChrisyXx|xXChrisyXx_room|house|0|12|RockinRebels|False|149345
+Zagan|Zagan_room|house|0|2|Fuck-A-Slut – Colds no Chat|False|48557
+Thralia|Thralia_room|house|2|2|Darkling Haven|False|60055
+Salazarr|Salazarr_room|house|0|2|BBC FOR SLUT|False|11111
```

Groups
invite to player: `Lzzy~ask/groupchat|f|177661`

when you accept invite `joingroup 207600`, `service mygroup=177661`, `service grouplist=Lzzy+EzekielTwentyFive+`
when player accepts invite: `service groupadd=Lzzy=f=`
when player leaves group `service groupleave=Lzzy=`
when you leave group `service mygroup=207652`

???
when you send invite `service mygroup=177661`, `service grouplist=Lzzy+`
