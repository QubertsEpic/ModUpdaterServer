using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Numerics;

namespace Server.ModCatalogue
{
    internal class Mod
    {
        public string Filename { get => filename; }
        private string filename;
        public byte[] Hash { get => hash; }
        private byte[] hash;
        public long FileSize { get => fileSize; }
        private long fileSize;
        public int ID { get => id; }
        private int id;
        public DateTime CreationTime { get => creationTime; }
        private DateTime creationTime;

        public bool anyNull
        {
            get
            {
                return DateTime.Compare(CreationTime, DateTime.Now) >= 0 || FileSize < 0 || hash.Length < 1 || filename.Length < 1;
            }
        }

        public Mod(string fileLocation, int id)
        {
            if (!File.Exists(fileLocation))
            {
                throw new FileNotFoundException(fileLocation + " does not exist.");
            }
            this.filename = Path.GetFileName(fileLocation);
            FileInfo fileinfo = new FileInfo(fileLocation);
            this.fileSize = fileinfo.Length;
            this.creationTime = fileinfo.CreationTime;
            this.hash = ComputeHash(fileLocation);
            this.id = id;
        }

        private Mod(int id,string fileName, byte[] hash, long filesize, DateTime lastModified)
        {
            this.id = id;
            this.filename = fileName;
            this.hash = hash;
            this.fileSize = filesize;
            this.creationTime = lastModified;
        }

        public static ModState VerifyMod(Mod mod, string folder)
        {
            if (mod == null)
            {
                return ModState.Outdated;
            }

            string modLocation = Path.Combine(folder, mod.Filename);

            //Check if the file exists, if it doesn't update the mod.
            if (!File.Exists(modLocation))
            {
                return ModState.Outdated;
            }

            FileInfo info = new FileInfo(modLocation);
            if(DateTime.Compare(info.CreationTime, mod.CreationTime) < 0)
            {
                return ModState.Outdated;
            }

            return ModState.Updated;
        }
                

        private static byte[] ComputeHash(string filelocation)
        {
            byte[] data = File.ReadAllBytes(filelocation);
            byte[] hash = MD5.Create().ComputeHash(data);
            return hash;
        }


        public byte[] GetSerialised()
        {
            //Check if any of the data being serialised is null.
            if (anyNull)
            {
                throw new NullReferenceException("Data is null, cannot serialise. Please attempt to reconstruct this class or dispose of it, as there is a problem with it.");
            }

            //Open the stream and writer.
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            //write all the data in a way that will be read later by the same code.
            writer.Write(id);
            writer.Write(filename);
            writer.Write(fileSize);
            writer.Write(creationTime.ToBinary());
            writer.Write(hash.Length);
            writer.Flush();

            stream.Write(hash, 0, hash.Length);
            stream.Flush();

            byte[] bytes = stream.ToArray();

            //Close the stream.
            stream.Close();

            return bytes;
        }

        public static Mod FromSerialised(byte[] data)
        {
            //Open the stream and writer.
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);
            
            //Read all of the data in the order it is supposed to be in
            int id = reader.ReadInt32();
            string filename = reader.ReadString();
            long filesize = reader.ReadInt64();
            //Read last date and time.
            DateTime lastMod = DateTime.FromBinary(reader.ReadInt64());

            int length = reader.ReadInt32();
            byte[] hash = new byte[length];
            reader.Read(hash);
            reader.Close();
            //Construct a new Mod class from the data we extracted.
            return new Mod(id,filename, hash, filesize, lastMod);
        }
    }

    public enum ModState
    {
        Updated,
        Outdated
    }
}