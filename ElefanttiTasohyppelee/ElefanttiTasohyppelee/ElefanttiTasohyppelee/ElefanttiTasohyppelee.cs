using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;
/// @author Anette
/// @version 7.4.2016
/// <summary>
/// Kärsäkäs elefanttitasohyppely peli. Tarkoituksena on ohjata norsun poikasta pimeässä, öisessä
/// viidakossa ja auttaa keräämään ananaksia, joiden avulla pentu elää yöllä ja jatkaa seikkailuaan
/// viidakon oksilla, koittaen löytää kotiin emon luokse.
/// </summary>

public class ElefanttiTasohyppelee : PhysicsGame
{
    const double nopeus = 200;
    const double hyppyNopeus = 750;
    const int RUUDUN_KOKO = 20;

    /// <summary>
    /// Luodaan itse kentän ympäristö, ananakset ja pelaajat sekä tausta pelille.
    /// </summary>
    PlatformCharacter norsu1;
    Image norsunKuva = LoadImage("norsu");
    Image ananasKuva = LoadImage("ananas");
    Image oksaKuva = LoadImage("oksa");
    private List<Label> valikonKohdat;
    IntMeter keratytEsineet;
    Timer ajastin;

    /// <summary>
    /// Luodaan kenttä, valikko ja näppäimet, yms. peliä varten.
    /// </summary>
    public override void Begin()
    {
        Valikko();
        Mouse.IsCursorVisible = true;
    }

    /// <summary>
    /// Luodaan itse kentän ympäristö (tekstitiedosto), ananakset ja pelaajat, maali sekä tausta pelille.
    /// </summary>
    void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta1");
        kentta.SetTileMethod('+', LisaaTaso);
        kentta.SetTileMethod('#', LisaaOksa);
        kentta.SetTileMethod('*', LisaaAnanas);
        kentta.SetTileMethod('N', LisaaNorsu);
        kentta.SetTileMethod('M', Maali);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        Level.CreateBorders();
        Level.Background.CreateStars();
        Layers[-3].RelativeTransition = new Vector(0.5, 0.5);
        Gravity = new Vector(0, -1000);

    }

    /// <summary>
    /// Luodaan pelille maali, johon peli päättyy norsun sinne törmättyä.
    /// </summary>
    void Maali(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject maali = PhysicsObject.CreateStaticObject(leveys, korkeus);
        maali.Position = paikka;
        maali.Tag = "maali";
        Add(maali);
    }

    /// <summary>
    /// Luodaan viidakon oksa, eli tasohyppelyn uusi hyppäuksen taso pelaajalle, peliin.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    void LisaaOksa(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject oksa = PhysicsObject.CreateStaticObject(75.0, 20.0);
        oksa.Position = paikka;
        oksa.Image = oksaKuva;
        oksa.Tag = "oksa";
        Add(oksa);
    }

    /// <summary>
    /// Luodaan alataso pelille, siis ns. maa mistä peli lähtee liikkeelle ja josta norsua lähdetään viemään
    /// viidakon yläilmoihin.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Olive;
        taso.Tag = "taso";
        Add(taso);
    }

    /// <summary>
    /// Luodaan kerättävä hedelmä, ananas, joita poimimalla saa norsupelaaja saa pisteitä,
    /// ja norsupoikanen pysyy hengissä.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    void LisaaAnanas(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject ananas = PhysicsObject.CreateStaticObject(leveys, korkeus);
        ananas.IgnoresCollisionResponse = true;
        ananas.Position = paikka;
        ananas.Image = ananasKuva;
        ananas.Tag = "ananas";
        Add(ananas);
    }

    /// <summary>
    /// Tehdään pelaaja kentälle, norsu1, joka liikkuu pelissä ja kerää ananaksia pysyäkseen hengissä. 
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    void LisaaNorsu(Vector paikka, double leveys, double korkeus)
    {
        norsu1 = new PlatformCharacter(30.0, 30.0);
        norsu1.Position = paikka;
        norsu1.Mass = 4.0;
        norsu1.Image = norsunKuva;
        AddCollisionHandler(norsu1, "ananas", TormaaAnanas);
        AddCollisionHandler(norsu1, "taso", NorsuTormasi);
        AddCollisionHandler(norsu1, "maali", NorsuMaalissa);
        Add(norsu1);
    }

    /// <summary>
    /// Lisätään näppäinten toimivuudet peliin, eli että millä näppäimellä pelaaja etenee minnekin, ja miten pelistä poistutaan,
    /// ja kuinka saadaan ohjeet näkyviin, yms.
    /// </summary>
    void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä kärsäkkään peliohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", norsu1, -nopeus);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", norsu1, nopeus);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Norsu hyppää", norsu1, hyppyNopeus);

        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Exit, "Poistu pelistä");

        ControllerOne.Listen(Button.DPadLeft, ButtonState.Down, Liikuta, "Norsu liikkuu vasemmalle", norsu1, -nopeus);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Down, Liikuta, "Norsu liikkuu oikealle", norsu1, nopeus);
        ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Norsu hyppää", norsu1, hyppyNopeus);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }

    /// <summary>
    /// Luodaan pelaajalle liikuvuus, eli kävelynopeus.
    /// </summary>
    /// <param name="norsu"></param>
    /// <param name="nopeus"></param>
    void Liikuta(PlatformCharacter norsu, double nopeus)
    {
        norsu.Walk(nopeus);
    }

    /// <summary>
    /// Luodaan pelaajalle hyppynopeus
    /// </summary>
    /// <param name="norsu"></param>
    /// <param name="nopeus"></param>
    void Hyppaa(PlatformCharacter norsu, double nopeus)
    {
        norsu.Jump(nopeus);
    }

    /// <summary>
    /// Luodaan pelaajalle törmäys ananakseen
    /// </summary>
    /// <param name="norsu"></param>
    /// <param name="ananas"></param>
    void TormaaAnanas(PhysicsObject norsu, PhysicsObject ananas)
    {
        ananas.Destroy();
        keratytEsineet.Value += 1;
    }

    /// <summary>
    /// Luodaan pelaajan kuolema ja pelin aloitus alusta
    /// </summary>
    /// <param name="norsu"></param>
    /// <param name="taso"></param>
    void NorsuTormasi(PhysicsObject norsu, PhysicsObject taso)
    {
        Explosion rajahdys = new Explosion(50);
        rajahdys.Position = norsu.Position;
        Add(rajahdys);
        norsu.Destroy();
        ajastin = new Timer();
        ajastin.Interval = 1.5;
        ajastin.Timeout += Valikko;
        ajastin.Start();
        // Valikko();

    }

    /// <summary>
    /// Norsu pääsee maaliin
    /// </summary>
    void NorsuMaalissa(PhysicsObject norsu, PhysicsObject maali)
    {
        norsu.Destroy();
        Valikko();

    }

    /// <summary>
    /// Luodaan pelin aloitus alusta, kun norsu kuolee tai pääsee maaliin.
    /// </summary>
    void AloitaAlusta()
    {
        ClearAll();
        LuoKentta();
        LisaaNappaimet();
        Camera.Follow(norsu1);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
        LuoPistelaskuri();

    }

    /// <summary>
    /// Luodaan pelin valikko, josta pelin voi aloittaa ja lopettaa
    /// </summary>
    void Valikko()
    {
        ClearAll();
        valikonKohdat = new List<Label>();

        Label kohta1 = new Label("Aloita Kärsäkäs peli");
        kohta1.Position = new Vector(0, 40);
        valikonKohdat.Add(kohta1);

        /*Label kohta2 = new Label("Parhaat pisteet");
        kohta2.Position = new Vector(0, 0);
        valikonKohdat.Add(kohta2);*/

        Label kohta3 = new Label("Lopeta peli");
        kohta3.Position = new Vector(0, -40);
        valikonKohdat.Add(kohta3);

        foreach (Label valikonKohta in valikonKohdat)
        {
            Add(valikonKohta);
        }

        Mouse.ListenOn(kohta1, MouseButton.Left, ButtonState.Pressed, AloitaAlusta, null);
        //Mouse.ListenOn(kohta2, MouseButton.Left, ButtonState.Pressed, ParhaatPisteet, null);
        Mouse.ListenOn(kohta3, MouseButton.Left, ButtonState.Pressed, Exit, null);
        Mouse.ListenMovement(1.0, ValikossaLiikkuminen, null);
    }

    /// <summary>
    /// Luodaan aliohjelma jonka avulla valikkossa liikkuminen näkyy
    /// </summary>
    void ValikossaLiikkuminen(AnalogState hiirenTila)
    {
        foreach (Label kohta in valikonKohdat)
        {
            if (Mouse.IsCursorOn(kohta))
            {
                kohta.TextColor = Color.Red;
            }
            else
            {
                kohta.TextColor = Color.Black;
            }

        }
    }

    IntMeter pisteLaskuri;

    void LuoPistelaskuri()
    {
        pisteLaskuri = new IntMeter(0);

        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 100;
        pisteNaytto.TextColor = Color.Black;
        pisteNaytto.Color = Color.White;
        pisteNaytto.Title = "Ananaksia kerätty";
        keratytEsineet = new IntMeter(0);

        pisteNaytto.BindTo(keratytEsineet);
        Add(pisteNaytto);
    }
}