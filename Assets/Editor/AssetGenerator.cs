#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

// Generates all game sprites programmatically.
// Run: IdleClicker > Generate Assets  (before or after Setup Scene)
public static class AssetGenerator
{
    const string OutPath = "Assets/Resources/Sprites/";

    // ── Entry point ──────────────────────────────────────────────────────────
    [MenuItem("IdleClicker/Generate Assets", priority = 2)]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(OutPath);

        RoundedRect();
        Background();
        Buttons();
        Hero();
        Goblin();
        OrcWarrior();
        CaveTroll();
        StoneOgre();
        DemonKnight();
        VampireLord();
        AncientDragon();

        AssetDatabase.Refresh();
        Debug.Log("[AssetGenerator] Done. Sprites in " + OutPath);
    }

    // ── Rounded rect (9-slice base for all panels/buttons) ───────────────────
    static void RoundedRect()
    {
        const int S = 48, R = 12;
        var px = new Color32[S * S];
        var white = C(255, 255, 255, 255);
        var clear = C(0, 0, 0, 0);

        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            bool inCornerX = x < R || x >= S - R;
            bool inCornerY = y < R || y >= S - R;
            if (inCornerX && inCornerY)
            {
                int cx = x < R ? R : S - R - 1;
                int cy = y < R ? R : S - R - 1;
                float dx = x - cx, dy = y - cy;
                Set(px, S, x, y, dx * dx + dy * dy <= (float)R * R ? white : clear);
            }
            else
            {
                Set(px, S, x, y, white);
            }
        }

        var tex  = MakeTex(S, S, px);
        var path = OutPath + "rounded_rect.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType         = TextureImporterType.Sprite;
        imp.filterMode          = FilterMode.Bilinear;
        imp.mipmapEnabled       = false;
        imp.alphaIsTransparency = true;
        imp.spriteBorder        = new Vector4(R, R, R, R);
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.SaveAndReimport();
    }

    // ── Background ───────────────────────────────────────────────────────────
    static void Background()
    {
        int W = 256, H = 512;
        var px = new Color32[W * H];

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float t    = (float)y / H;
            float n    = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 18f;
            byte  r    = (byte)Mathf.Clamp(Lerp(20, 8,  t) + n, 0, 255);
            byte  g    = (byte)Mathf.Clamp(Lerp(8,  3,  t) + n * 0.4f, 0, 255);
            byte  b    = (byte)Mathf.Clamp(Lerp(30, 12, t) + n, 0, 255);
            px[y * W + x] = C(r, g, b);
        }

        // Blood drips
        for (int y = 10; y < 90; y++)
        {
            int x = 28 + (int)(Mathf.Sin(y * 0.22f) * 3);
            DotPx(px, W, H, x, y, C(110, 4, 4, 190));
            DotPx(px, W, H, x + 1, y, C(110, 4, 4, 190));
        }
        for (int y = 30; y < 130; y++)
        {
            int x = 218 + (int)(Mathf.Sin(y * 0.18f) * 4);
            DotPx(px, W, H, x, y, C(90, 3, 3, 170));
        }

        var tex = MakeTex(W, H, px);
        Save(tex, OutPath + "background.png", sprite: false);
    }

    // ── Buttons (9-slice, 64×32) ─────────────────────────────────────────────
    static void Buttons()
    {
        GenButton("btn_normal",   C(42, 32, 52),  C(82,  62, 105), C(122, 92, 155));
        GenButton("btn_pressed",  C(28, 20, 38),  C(58,  44, 74),  C(88,  68, 108));
        GenButton("btn_disabled", C(32, 30, 34),  C(52,  50, 54),  C(72,  70, 74));
    }

    static void GenButton(string name, Color32 dark, Color32 mid, Color32 light)
    {
        int W = 64, H = 32;
        var px = new Color32[W * H];

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            Set(px, W, H, x, y, mid);

        for (int x = 0; x < W; x++) { Set(px, W, H, x, 0,   light); Set(px, W, H, x, 1,   Lerp32(mid, light, 0.5f)); }
        for (int x = 0; x < W; x++) { Set(px, W, H, x, H-1, dark);  Set(px, W, H, x, H-2, Lerp32(mid, dark,  0.5f)); }
        for (int y = 0; y < H; y++) { Set(px, W, H, 0, y, dark); Set(px, W, H, W-1, y, dark); }

        // Round corners
        for (int d = 0; d < 4; d++) for (int e = 0; e < 4 - d; e++)
        {
            Set(px, W, H, d,     e,     C(0,0,0,0));
            Set(px, W, H, W-1-d, e,     C(0,0,0,0));
            Set(px, W, H, d,     H-1-e, C(0,0,0,0));
            Set(px, W, H, W-1-d, H-1-e, C(0,0,0,0));
        }

        var tex = MakeTex(W, H, px);
        var path = OutPath + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType      = TextureImporterType.Sprite;
        imp.filterMode       = FilterMode.Point;
        imp.mipmapEnabled    = false;
        imp.alphaIsTransparency = true;
        imp.spriteBorder     = new Vector4(12, 8, 12, 8);
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.SaveAndReimport();
    }

    // ── Hero ─────────────────────────────────────────────────────────────────
    static void Hero()
    {
        int S = 48;
        var px = Blank(S);

        var skinLt  = C(255, 200, 140); var skinDk = C(185, 125, 72);
        var hairDk  = C(55,  32,  12);  var hairMd = C(85, 52, 22);
        var armrBl  = C(25,  85, 178);  var armrDk = C(14, 54, 112);
        var armrSh  = C(80, 142, 222);
        var swrdGy  = C(182, 192, 202); var swrdSh = C(232, 242, 255);
        var gold    = C(222, 172, 30);
        var ol      = C(20,  10,   5);

        // Legs
        Dot(px, S, 18, 42, 5, ol);   Dot(px, S, 18, 42, 4, armrDk);
        Dot(px, S, 30, 42, 5, ol);   Dot(px, S, 30, 42, 4, armrDk);

        // Body
        Oval(px, S, 24, 32, 11, 10, ol);   Oval(px, S, 24, 32, 10, 9, armrBl);
        Oval(px, S, 22, 29,  5,  4, armrSh);

        // Shield (left)
        Dot(px, S, 10, 30, 7, armrDk); Dot(px, S, 10, 30, 6, armrBl);
        Dot(px, S,  9, 29, 2, armrSh); Dot(px, S, 10, 30, 1, gold);

        // Sword (right)
        Line(px, S, 37, 8, 44, 40, C(90,100,110), 2);
        Line(px, S, 36, 8, 43, 40, swrdGy, 1);
        Line(px, S, 35, 9, 42, 38, swrdSh, 1);
        Rect(px, S, 32, 22, 8, 3, gold);

        // Head
        Dot(px, S, 24, 14, 10, ol);   Dot(px, S, 24, 14, 9, skinLt);
        // Hair
        Rect(px, S, 15, 4, 18, 8, ol);
        Rect(px, S, 16, 5, 16, 6, hairMd);
        Rect(px, S, 16, 4, 16, 3, hairDk);
        // Eyes
        Dot(px, S, 20, 13, 2, C(255,255,255)); Set(px,S,S,20,13,C(50,80,180));
        Dot(px, S, 28, 13, 2, C(255,255,255)); Set(px,S,S,28,13,C(50,80,180));
        // Smile
        for (int x = 21; x < 28; x++) Set(px, S, S, x, 19, skinDk);
        Set(px, S, S, 21, 18, skinDk); Set(px, S, S, 27, 18, skinDk);

        Save(MakeTex(S, S, px), OutPath + "hero.png");
    }

    // ── Enemies ──────────────────────────────────────────────────────────────
    static void Goblin()
    {
        int S = 48; var px = Blank(S);
        var lt = C(132,192,60); var md = C(100,160,40); var dk = C(62,112,20);
        var ol = C(20,40,5);   var rd = C(220,30,30);
        var wh = C(240,240,200); var pk = C(180,100,100);
        var br = C(100,65,20); var bdk = C(60,35,10);

        Oval(px,S, 24,36, 10,9, ol);  Oval(px,S, 24,36, 9,8, dk);
        Dot(px,S, 24,17, 12,ol);      Dot(px,S, 24,17, 11,md);
        Dot(px,S, 22,15,  6,lt);
        Dot(px,S, 10,14,  4,ol); Dot(px,S, 10,14, 3,md); Dot(px,S, 10,14, 2,pk);
        Dot(px,S, 38,14,  4,ol); Dot(px,S, 38,14, 3,md); Dot(px,S, 38,14, 2,pk);
        Dot(px,S, 19,15, 3,C(240,240,240)); Dot(px,S, 19,15, 2,rd); Dot(px,S, 19,15, 1,C(10,10,10));
        Dot(px,S, 29,15, 3,C(240,240,240)); Dot(px,S, 29,15, 2,rd); Dot(px,S, 29,15, 1,C(10,10,10));
        Line(px,S, 18,23, 30,23, ol, 1);
        Set(px,S,S,20,23,wh); Set(px,S,S,21,22,wh); Set(px,S,S,22,23,wh);
        Set(px,S,S,26,23,wh); Set(px,S,S,27,22,wh); Set(px,S,S,28,23,wh);
        Line(px,S, 39,22, 44,40, bdk, 3); Line(px,S, 39,22, 44,40, br, 2);
        Dot(px,S, 40,19, 5,bdk); Dot(px,S, 40,19, 4,C(80,50,15));
        Oval(px,S, 19,45, 5,3, ol); Oval(px,S, 19,45, 4,2, dk);
        Oval(px,S, 29,45, 5,3, ol); Oval(px,S, 29,45, 4,2, dk);
        Save(MakeTex(S,S,px), OutPath + "goblin.png");
    }

    static void OrcWarrior()
    {
        int S = 48; var px = Blank(S);
        var sk = C(82,118,52); var sklt = C(112,158,72); var skdk = C(56,82,30);
        var am = C(90,96,106); var amlt = C(142,148,158); var amdk = C(50,56,66);
        var tsk = C(242,228,182); var rd = C(200,30,30);
        var ol = C(20,26,10);  var wd = C(100,65,20); var ax = C(70,102,162);

        // Body & chest plate
        Oval(px,S, 24,36, 13,11, ol); Oval(px,S, 24,36, 12,10, am);
        Oval(px,S, 23,33,  6, 5, amlt); Line(px,S, 24,26, 24,44, amdk, 1);
        // Head
        Dot(px,S, 24,15, 13,ol); Dot(px,S, 24,15, 12,sk); Dot(px,S, 22,12, 6,sklt);
        // Helmet
        Oval(px,S, 24,7, 13,7, ol); Oval(px,S, 24,7, 12,6, am);
        Line(px,S, 13,6, 8,0, amdk, 2); Line(px,S, 35,6, 40,0, amdk, 2);
        // Tusks
        Oval(px,S, 19,26, 3,4, C(200,185,140)); Oval(px,S, 19,26, 2,3, tsk);
        Oval(px,S, 29,26, 3,4, C(200,185,140)); Oval(px,S, 29,26, 2,3, tsk);
        // Eyes
        Dot(px,S, 18,13, 3,C(230,230,230)); Dot(px,S, 18,13, 2,rd); Dot(px,S, 18,13, 1,C(5,5,5));
        Dot(px,S, 30,13, 3,C(230,230,230)); Dot(px,S, 30,13, 2,rd); Dot(px,S, 30,13, 1,C(5,5,5));
        // Axe
        Line(px,S, 8,10, 15,38, C(70,45,15), 3); Line(px,S, 8,10, 15,38, wd, 2);
        Oval(px,S, 6,12, 7,10, ol); Oval(px,S, 6,12, 6,9, ax); Dot(px,S, 5,11, 3,C(152,182,222));
        // Legs
        Oval(px,S, 18,45, 6,4, ol); Oval(px,S, 18,45, 5,3, amdk);
        Oval(px,S, 30,45, 6,4, ol); Oval(px,S, 30,45, 5,3, amdk);
        Save(MakeTex(S,S,px), OutPath + "orc_warrior.png");
    }

    static void CaveTroll()
    {
        int S = 48; var px = Blank(S);
        var bs = C(92,118,82); var lt = C(132,162,112); var dk = C(56,78,46);
        var yw = C(222,202,32); var ol = C(26,36,16);
        var rk = C(122,122,122);

        // Body
        Oval(px,S, 24,34, 15,13, ol); Oval(px,S, 24,34, 14,12, dk);
        Oval(px,S, 22,30,  8, 7, bs);
        // Head
        Oval(px,S, 24,14, 14,12, ol); Oval(px,S, 24,14, 13,11, bs);
        Oval(px,S, 22,11,  7, 6, lt);
        // Rock weapon
        Line(px,S, 36,28, 40,22, dk, 4); Line(px,S, 36,28, 40,22, bs, 2);
        Dot(px,S, 42,20, 7,C(80,80,80)); Dot(px,S, 42,20, 6,rk); Dot(px,S, 40,18, 3,C(182,182,182));
        // Eyes
        Dot(px,S, 18,11, 3,C(30,30,20)); Dot(px,S, 18,11, 2,yw); Dot(px,S, 18,11, 1,C(10,10,5));
        Dot(px,S, 30,11, 3,C(30,30,20)); Dot(px,S, 30,11, 2,yw); Dot(px,S, 30,11, 1,C(10,10,5));
        // Nose
        Oval(px,S, 24,17, 4,3, dk); Dot(px,S, 22,16, 2,lt);
        // Mouth + teeth
        Line(px,S, 15,22, 33,22, ol, 2);
        for (int x = 16; x < 33; x += 4) { Set(px,S,S,x,21,C(220,215,195)); Set(px,S,S,x,20,C(220,215,195)); }
        // Mossy patches
        Dot(px,S, 17,36, 3,C(62,102,32)); Dot(px,S, 32,30, 2,C(52,92,26)); Dot(px,S, 20,28, 2,C(72,112,36));
        // Feet
        Oval(px,S, 16,46, 7,4, ol); Oval(px,S, 16,46, 6,3, dk);
        Oval(px,S, 32,46, 7,4, ol); Oval(px,S, 32,46, 6,3, dk);
        Save(MakeTex(S,S,px), OutPath + "cave_troll.png");
    }

    static void StoneOgre()
    {
        int S = 48; var px = Blank(S);
        var sk = C(187,147,97); var sklt = C(217,188,142); var skdk = C(142,107,67);
        var cl = C(142,26,26); var br = C(92,62,26); var yw = C(232,142,22);
        var ol = C(62,42,22);  var sp = C(152,142,122);

        Oval(px,S, 23,35, 14,12, ol); Oval(px,S, 23,35, 13,11, sk);
        Rect(px,S, 10,38, 26,4, cl); Oval(px,S, 21,31, 7,6, sklt);
        Dot(px,S, 23,13, 13,ol); Dot(px,S, 23,13, 12,sk); Dot(px,S, 21,10, 7,sklt);
        Dot(px,S, 17,11, 3,C(30,20,10)); Dot(px,S, 17,11, 2,yw); Dot(px,S, 17,11, 1,C(5,5,5));
        Dot(px,S, 29,11, 3,C(30,20,10)); Dot(px,S, 29,11, 2,yw); Dot(px,S, 29,11, 1,C(5,5,5));
        Oval(px,S, 23,18, 5,4, skdk); Dot(px,S, 20,16, 2,sklt);
        Dot(px,S, 14,14, 2,skdk); Dot(px,S, 34,12, 2,skdk); Dot(px,S, 28,19, 2,skdk);
        Line(px,S, 13,23, 33,24, ol, 2);
        Set(px,S,S,17,22,sklt); Set(px,S,S,17,21,sklt); Set(px,S,S,22,23,sklt);
        Set(px,S,S,28,22,sklt); Set(px,S,S,28,21,sklt);
        // Spiked club
        Line(px,S, 38,44, 43,14, C(62,42,16), 3); Line(px,S, 38,44, 43,14, br, 2);
        Dot(px,S, 43,12, 5,C(72,67,57));
        for (int a = 0; a < 360; a += 60)
        {
            int sx = 43+(int)(Mathf.Cos(a*Mathf.Deg2Rad)*6), sy = 12+(int)(Mathf.Sin(a*Mathf.Deg2Rad)*6);
            Dot(px,S,sx,sy,2,sp);
        }
        Oval(px,S, 15,46, 7,3, ol); Oval(px,S, 15,46, 6,2, skdk);
        Oval(px,S, 31,46, 7,3, ol); Oval(px,S, 31,46, 6,2, skdk);
        Save(MakeTex(S,S,px), OutPath + "stone_ogre.png");
    }

    static void DemonKnight()
    {
        int S = 48; var px = Blank(S);
        var ab = C(20,15,25); var ar = C(162,10,10); var go = C(255,122,0);
        var gr = C(255,52,0);  var ms = C(102,88,122);
        var ol = C(10,5,15);   var bd = C(182,172,202); var bg = C(222,52,255);

        Oval(px,S, 24,34, 13,12, ol); Oval(px,S, 24,34, 12,11, ab);
        for (int i=0;i<3;i++) { Set(px,S,S,24,26+i*4,ar); Set(px,S,S,23,26+i*4,ar); Set(px,S,S,25,26+i*4,ar); }
        Oval(px,S, 22,30, 5,5, ar); Dot(px,S, 22,30, 3,C(100,5,5)); Dot(px,S, 22,30, 1,go);
        Dot(px,S, 24,13, 12,ol); Dot(px,S, 24,13, 11,ab);
        Line(px,S, 14,8, 6,0, ar, 3);  Line(px,S, 34,8, 42,0, ar, 3);
        Rect(px,S, 15,11, 18,4, C(5,5,10));
        Dot(px,S, 19,13, 3,go); Dot(px,S, 19,13, 2,gr);
        Dot(px,S, 29,13, 3,go); Dot(px,S, 29,13, 2,gr);
        Line(px,S, 7,8, 14,40, C(62,62,82), 3); Line(px,S, 7,8, 14,40, bd, 1);
        Line(px,S, 6,8, 13,40, C(102,0,152,80), 2); Line(px,S, 8,8, 15,40, C(102,0,152,80), 2);
        Rect(px,S, 5,22, 11,3, ms); Rect(px,S, 5,22, 11,2, C(142,132,162));
        Oval(px,S, 40,28, 7,9, ol); Oval(px,S, 40,28, 6,8, ab);
        Oval(px,S, 40,28, 4,6, ar); Dot(px,S, 40,28, 2,go);
        Oval(px,S, 18,45, 6,4, ol); Oval(px,S, 18,45, 5,3, ab);
        Oval(px,S, 30,45, 6,4, ol); Oval(px,S, 30,45, 5,3, ab);
        Save(MakeTex(S,S,px), OutPath + "demon_knight.png");
    }

    static void VampireLord()
    {
        int S = 48; var px = Blank(S);
        var sp = C(217,207,222); var ss = C(177,162,187);
        var co = C(52,10,72);   var ci = C(162,10,10);
        var hk = C(20,15,30);   var rd = C(222,20,20); var eg = C(255,82,82);
        var fn = C(247,242,252); var ol = C(15,8,25); var gd = C(202,162,20);

        Oval(px,S, 24,38, 18,14, ol); Oval(px,S, 24,38, 17,13, co);
        Oval(px,S, 24,40, 12,10, ci);
        Oval(px,S, 24,32,  9, 8, ol); Oval(px,S, 24,32, 8,7, C(32,22,42));
        Oval(px,S, 24,30,  4, 5, C(222,217,227));
        Dot(px,S, 24,28, 3,C(152,122,16)); Dot(px,S, 24,28, 2,gd); Dot(px,S, 24,28, 1,C(242,212,62));
        Dot(px,S, 24,13, 11,ol); Dot(px,S, 24,13, 10,sp); Dot(px,S, 22,11, 5,C(232,227,237));
        Rect(px,S, 14,2, 20,8, ol); Rect(px,S, 15,3, 18,6, hk); Dot(px,S, 24,5, 3,hk);
        Dot(px,S, 19,12, 3,C(22,12,27)); Dot(px,S, 19,12, 2,rd); Dot(px,S, 19,12, 1,eg);
        Dot(px,S, 29,12, 3,C(22,12,27)); Dot(px,S, 29,12, 2,rd); Dot(px,S, 29,12, 1,eg);
        Set(px,S,S,24,16,ss); Set(px,S,S,24,17,ss);
        Line(px,S, 19,20, 29,20, ss, 1);
        Set(px,S,S,21,21,fn); Set(px,S,S,21,22,fn); Set(px,S,S,21,23,rd);
        Set(px,S,S,27,21,fn); Set(px,S,S,27,22,fn); Set(px,S,S,27,23,rd);
        Oval(px,S, 24,22, 12,4, ol); Oval(px,S, 24,22, 11,3, co);
        Dot(px,S, 18,47, 4,ol); Dot(px,S, 18,47, 3,hk);
        Dot(px,S, 30,47, 4,ol); Dot(px,S, 30,47, 3,hk);
        Save(MakeTex(S,S,px), OutPath + "vampire_lord.png");
    }

    static void AncientDragon()
    {
        int S = 48; var px = Blank(S);
        var sr = C(182,22,16); var sl = C(232,62,32); var sdk = C(112,8,6);
        var bl = C(222,172,82); var blt = C(242,212,132);
        var wdk = C(92,8,6);   var wm  = C(152,16,10,202);
        var eg  = C(255,202,0); var es  = C(182,132,0);
        var fl  = C(255,168,0); var fh  = C(255,242,52);
        var ol  = C(52,5,5);   var cl  = C(202,192,162);

        // Wings (drawn first, behind body)
        Oval(px,S,  8,22, 9,14, wdk); Oval(px,S,  8,22, 8,13, wm);
        Oval(px,S, 40,22, 9,14, wdk); Oval(px,S, 40,22, 8,13, wm);

        // Flame (before body so head is on top)
        for (int fy = 0; fy < 18; fy++)
        {
            float t = (float)fy / 18f;
            int fw = Mathf.Max(1, (int)(3 * (1f - t)));
            byte a = (byte)(200 - t * 80);
            Color32 fc = fy < 6 ? fh : fy < 12 ? fl : C(200,82,0);
            Rect(px, S, 24 - fw, fy, fw * 2, 2, C(fc.r, fc.g, fc.b, a));
        }

        // Body
        Oval(px,S, 24,32, 13,11, ol);   Oval(px,S, 24,32, 12,10, sr);
        Oval(px,S, 22,29,  7, 6, sl);
        Oval(px,S, 25,35,  7, 7, C(192,142,62)); Oval(px,S, 25,35, 6,6, bl);
        Oval(px,S, 24,33,  4, 4, blt);
        for (int sy=26;sy<42;sy+=4) for (int sx=14;sx<34;sx+=4) Set(px,S,S,sx+(sy/4%2)*2,sy,sdk);

        // Neck + head
        Oval(px,S, 24,17, 10,8, ol); Oval(px,S, 24,17, 9,7, sr);
        Oval(px,S, 24,10, 12,8, ol); Oval(px,S, 24,10, 11,7, sr);
        Dot(px,S, 22, 8, 5,sl);
        Oval(px,S, 24,14,  6,4, ol); Oval(px,S, 24,14, 5,3, sr);
        Set(px,S,S,21,14,sdk); Set(px,S,S,27,14,sdk);

        // Eyes (slit pupils)
        Dot(px,S, 17,8, 3,C(22,12,6)); Dot(px,S, 17,8, 2,eg);
        Set(px,S,S,17,7,es); Set(px,S,S,17,8,es); Set(px,S,S,17,9,es);
        Dot(px,S, 31,8, 3,C(22,12,6)); Dot(px,S, 31,8, 2,eg);
        Set(px,S,S,31,7,es); Set(px,S,S,31,8,es); Set(px,S,S,31,9,es);
        Line(px,S, 16,4, 10,0, sdk, 2); Line(px,S, 32,4, 38,0, sdk, 2);

        // Claws
        for (int ci2=0;ci2<3;ci2++)
        {
            Line(px,S, 16+ci2*2,44, 14+ci2*2,48, ol,  1);
            Line(px,S, 16+ci2*2,44, 14+ci2*2,48, cl, 1);
            Line(px,S, 30+ci2*2,44, 28+ci2*2,48, ol,  1);
            Line(px,S, 30+ci2*2,44, 28+ci2*2,48, cl, 1);
        }
        Save(MakeTex(S,S,px), OutPath + "ancient_dragon.png");
    }

    // ── Drawing primitives ───────────────────────────────────────────────────

    // All draw calls use y=0=TOP convention; Set() flips to Unity's bottom-left origin.
    static void Set(Color32[] px, int W, int H, int x, int y, Color32 c)
    {
        if (x >= 0 && x < W && y >= 0 && y < H)
            px[(H - 1 - y) * W + x] = c;
    }

    // Overload for square textures (common case)
    static void Set(Color32[] px, int S, int x, int y, Color32 c) => Set(px, S, S, x, y, c);

    static void Dot(Color32[] px, int S, int cx, int cy, int r, Color32 c)
    {
        int r2 = r * r;
        for (int y = cy-r; y <= cy+r; y++)
        for (int x = cx-r; x <= cx+r; x++)
            if ((x-cx)*(x-cx)+(y-cy)*(y-cy) <= r2) Set(px, S, x, y, c);
    }

    static void Oval(Color32[] px, int S, int cx, int cy, int rx, int ry, Color32 c)
    {
        for (int y = cy-ry; y <= cy+ry; y++)
        for (int x = cx-rx; x <= cx+rx; x++)
        {
            float dx = (float)(x-cx)/rx, dy = (float)(y-cy)/ry;
            if (dx*dx+dy*dy <= 1f) Set(px, S, x, y, c);
        }
    }

    static void Rect(Color32[] px, int S, int x, int y, int w, int h, Color32 c)
    {
        for (int iy = y; iy < y+h; iy++)
        for (int ix = x; ix < x+w; ix++)
            Set(px, S, ix, iy, c);
    }

    static void Line(Color32[] px, int S, int x0, int y0, int x1, int y1, Color32 c, int t = 1)
    {
        int dx = Math.Abs(x1-x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1-y0), sy = y0 < y1 ? 1 : -1, err = dx+dy;
        for (int guard = 0; guard < 512; guard++)
        {
            Dot(px, S, x0, y0, t/2, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2*err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    // DotPx: direct (non-flipped) write for background gradient
    static void DotPx(Color32[] px, int W, int H, int x, int y, Color32 c)
    {
        if (x >= 0 && x < W && y >= 0 && y < H) px[y * W + x] = c;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r,(byte)g,(byte)b,(byte)a);
    static float   Lerp(float a, float b, float t)     => a + (b - a) * t;
    static Color32 Lerp32(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);
    static Color32[] Blank(int S)                       => new Color32[S * S];

    static Texture2D MakeTex(int W, int H, Color32[] px)
    {
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    static void Save(Texture2D tex, string path, bool sprite = true)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType         = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        imp.filterMode          = FilterMode.Point;
        imp.mipmapEnabled       = false;
        imp.alphaIsTransparency = sprite;
        if (sprite) imp.spriteImportMode = SpriteImportMode.Single;
        imp.SaveAndReimport();
    }
}
#endif
