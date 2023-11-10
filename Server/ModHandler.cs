using Server;
using Server.ModCatalogue;

internal class ModHandler
{
    const int Identifier = 502087;
    public Dictionary<int, Mod> Mods { get => mods;  }
    Dictionary<int, Mod> mods = new Dictionary<int, Mod>();
    FolderTracker FTracker { get; set; } = new FolderTracker();
    string folder => FTracker.FolderLocation;
    const string catName = "catalogue";
    const string catExt = "dat";
    const string modFileType = ".jar";
    string CatalogueFullname => catName  + catExt;
    public int Count => mods?.Count ?? 0;
    public ModHandler()
    {
        string fileLocation = Path.Combine(folder, CatalogueFullname);

        if (!File.Exists(fileLocation))
        {
            UpdateCatalogue();
        }

        FileStream stream = File.OpenRead(fileLocation);
        FromSerialised(stream);

        stream.Close();
    }
    public void UpdateCatalogue()
    {
        Dictionary<int, Mod> newMods = CatalogueMods();
        mods = newMods;
        ToFile();
    }

    private Dictionary<int, Mod> CatalogueMods()
    {
        string[] files = Directory.GetFiles(folder);
        Dictionary<int, Mod> mods = new Dictionary<int, Mod>();

        for(int i = 0; i < files.Length; i++)
        {
            if (!Path.GetExtension(files[i]).Equals(modFileType))
                continue;
            int id = GenerateId();
            Mod newMod = new Mod(files[i], id);
            Console.WriteLine("Mod \"" + newMod.Filename + "\" was catalogued.");
            if (string.IsNullOrEmpty(newMod.Filename) || newMod.FileSize < 1 || newMod.Hash == null)
                throw new Exception("Mod was not initialised properly.");
            mods.Add(id,newMod);
        }
        return mods;
    }

    public int GenerateId()
    {
        Random random = new Random();
        int randomId;
        do
        {
            randomId = random.Next();
        } while (mods?.ContainsKey(randomId) == true);
        return randomId;
    }

    public void ToFile()
    {
        FileStream stream = File.Open(Path.Combine(folder, CatalogueFullname), FileMode.Create);
        byte[] bytes = GetBytes(mods);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        stream.Close();
    }

    public byte[] GetBytes(Dictionary<int, Mod> mods)
    {
        if(mods == null)
        {
            throw new NullReferenceException("Cannot write mods if they do not exist.");
        }

        //
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(mods.Count());

        writer.Flush();

        foreach(var mod in mods)
        {
            writer.Write(mod.Key);
            byte[] buffer = mod.Value.GetSerialised();
            writer.Write(buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        stream.Flush();
        byte[] data = stream.ToArray();
        stream.Close();

        return data;
    }

    public Dictionary<int, byte[]> GetModData(Dictionary<int, Mod> mods)
    {
        return GetModData(mods.Keys.ToList());
    }

    public Dictionary<int, byte[]> GetModData(List <int> modIds)
    {
        Dictionary<int, byte[]> data = new Dictionary<int, byte[]>();
        for(int i = 0; i <  modIds.Count; i++)
        {
            if (!Mods.ContainsKey(modIds[i]))
                continue;
            Mod mod = Mods[modIds[i]];
            string path = Path.Combine(folder, mod.Filename);
            if (!File.Exists(path))
                continue;

            byte[] bytes = File.ReadAllBytes(path);
            data.Add(mod.ID,bytes);
        }
        return data;
    }
    public Dictionary<int,Mod> SelectedMods(byte[] data)
    {
        MemoryStream stream = new MemoryStream(data);
        stream.Position = 0;
        BinaryReader reader = new BinaryReader(stream);

        int length = reader.ReadInt32();
        Dictionary<int ,Mod> mods = new Dictionary<int, Mod>();
        for(int i= 0; i < length; i++)
        {
            int keys = reader.ReadInt32();
            if (this.mods?.ContainsKey(keys) == true)
            {
                Mod mod = this.mods[keys];
                mods.Add(keys,mod);
            }
        }

        return mods;
    }

    public void FromSerialised(FileStream stream)
    {
        BinaryReader reader = new BinaryReader(stream);

        int length = reader.ReadInt32();

        mods = new Dictionary<int, Mod>();

        for(int i = 0; i < length; i++)
        {
            int modID = reader.ReadInt32();
            int modLength = reader.ReadInt32();
            byte[] buffer = new byte[modLength];
            stream.Read(buffer, 0, buffer.Length);
            Mod newmod = Mod.FromSerialised(buffer);
            mods.Add(modID, newmod);
        }
    }
}