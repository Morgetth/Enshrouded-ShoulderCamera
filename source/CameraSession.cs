using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ShoulderCamera {
    internal struct CameraProfile {
        internal readonly bool Enabled;
        internal readonly float Horizontal,Height;
        internal CameraProfile(bool enabled,float horizontal,float height) {Enabled=enabled;Horizontal=horizontal;Height=height;}
    }
    // Experimental backend for one audited build. No code injection, hooks or file patches.
    internal sealed class CameraSession : IDisposable {
        internal const int Count=46, Stride=1768, Size=Count*Stride;
        const string BuildHash="82e5020a8736348cd0374f004b2f5e5ef456b5bd27aa5420df34c7dd241cfeeb";
        static readonly int[] Basic={4,5,6,7,16,17};
        static readonly int[] Bow={8,9};
        static readonly int[] Staff={10,11,13,14};
        IntPtr handle;
        readonly ulong manager;
        readonly byte[] original;
        readonly Dictionary<int,float> last=new Dictionary<int,float>();
        readonly HashSet<int> dirty=new HashSet<int>();
        bool disposed;
        internal ulong Address { get {return manager;} }

        [StructLayout(LayoutKind.Sequential)] struct Region {
            public IntPtr BaseAddress,AllocationBase;
            public uint AllocationProtect;
            public ushort PartitionId;
            public UIntPtr RegionSize;
            public uint State,Protect,Type;
        }
        [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr p,byte[] b,UIntPtr n,out UIntPtr read);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr p,byte[] b,UIntPtr n,out UIntPtr written);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr p,UIntPtr n,uint protection,out uint old);
        [DllImport("kernel32.dll")] static extern UIntPtr VirtualQueryEx(IntPtr h,IntPtr p,out Region r,UIntPtr n);
        [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr h,out uint code);
        [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint kind,uint protection);
        [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint kind);

        static Exception Failure(string action) {return new InvalidOperationException(action+" (Windows "+Marshal.GetLastWin32Error()+")");}
        internal bool IsAlive {get {uint code;return handle!=IntPtr.Zero && GetExitCodeProcess(handle,out code) && code==259;}}
        static bool Finite(float f) {return !float.IsNaN(f) && !float.IsInfinity(f);}
        internal static bool Plausible(byte[] data,int start,int length) {
            if(start<0 || length>data.Length || start>length-Size) return false;
            for(int i=0;i<Count;i++) {
                int p=start+i*Stride;
                if(data[p]!=i || data[p+908]>1 || data[p+909]>1 || data[p+44]>1) return false;
                for(int j=0;j<4;j++) {float f=BitConverter.ToSingle(data,p+884+4*j);if(!Finite(f) || Math.Abs(f)>100)return false;}
            }
            return true;
        }
        static byte[] Read(IntPtr h,ulong address,int length) {
            byte[] data=new byte[length]; UIntPtr count;
            if(!ReadProcessMemory(h,new IntPtr((long)address),data,(UIntPtr)(uint)length,out count) || count.ToUInt64()!=(ulong)length)
                throw Failure("Lecture camera refusee");
            return data;
        }
        static string Hash(string path) {
            using(var file=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
        }
        static ulong Find(IntPtr h) {
            const int chunk=1024*1024;
            ulong address=0,total=0; var timer=Stopwatch.StartNew(); var found=new HashSet<ulong>();
            while(address<0x00007fffffffffff && total<2UL*1024*1024*1024 && timer.Elapsed.TotalSeconds<30) {
                Region r;
                if(VirtualQueryEx(h,new IntPtr((long)address),out r,(UIntPtr)48)==UIntPtr.Zero)break;
                ulong begin=(ulong)r.BaseAddress.ToInt64(),size=r.RegionSize.ToUInt64(),next=begin+size;
                if(size==0 || next<=address)break;
                // The observed native resource is MEM_PRIVATE, PAGE_READONLY.
                if(r.State==0x1000 && r.Type==0x20000 && r.Protect==2 && size>=Size) {
                    for(ulong off=0;off<size && total<2UL*1024*1024*1024 && timer.Elapsed.TotalSeconds<30;off+=chunk) {
                        int n=(int)Math.Min((ulong)(chunk+Size-1),size-off);
                        if(n<Size)break;
                        byte[] b=new byte[n];UIntPtr read;
                        bool ok=ReadProcessMemory(h,new IntPtr((long)(begin+off)),b,(UIntPtr)(uint)n,out read);
                        total+=(ulong)n;
                        if(!ok || read.ToUInt64()!=(ulong)n)continue;
                        for(int i=0;i+Size<=n;i+=8) {
                            if(b[i]==0 && b[i+Stride]==1 && Plausible(b,i,n))found.Add(begin+off+(ulong)i);
                            if(found.Count>1)throw new InvalidOperationException("Plusieurs tableaux camera : connexion refusee.");
                        }
                    }
                }
                address=next;
            }
            if(found.Count!=1)throw new InvalidOperationException("Configuration camera introuvable dans la recherche limitee.");
            foreach(ulong candidate in found)return candidate;
            throw new InvalidOperationException();
        }
        CameraSession(IntPtr h,ulong address) {
            handle=h;manager=address;original=Read(h,address,Size);
            if(!Plausible(original,0,original.Length))throw new InvalidOperationException("Tableau camera incoherent.");
            foreach(int state in Basic)Register(state);
            foreach(int state in Bow)Register(state);
            foreach(int state in Staff)Register(state);
        }
        void Register(int state) {int p=state*Stride+884;last[p]=BitConverter.ToSingle(original,p);last[p+12]=BitConverter.ToSingle(original,p+12);}
        internal static CameraSession Connect() {
            var processes=Process.GetProcessesByName("enshrouded");
            try {
                if(processes.Length!=1)throw new InvalidOperationException("Ouvrez un seul client Enshrouded et chargez une partie.");
                if(Hash(processes[0].MainModule.FileName)!=BuildHash)throw new InvalidOperationException("Version du jeu non prise en charge. Aucune ecriture effectuee.");
                IntPtr h=OpenProcess(0x0400|0x0010|0x0020|0x0008,false,processes[0].Id);
                if(h==IntPtr.Zero)throw Failure("Connexion au jeu refusee");
                try {return new CameraSession(h,Find(h));}catch {CloseHandle(h);throw;}
            }finally {foreach(var p in processes)p.Dispose();}
        }
        void Ensure() {
            if(disposed || !IsAlive)throw new InvalidOperationException("Le jeu est ferme ou la connexion est perdue.");
            if(!Plausible(Read(handle,manager,Size),0,Size))throw new InvalidOperationException("Configuration camera remplacee : reconnectez le panneau.");
        }
        static bool Same(float a,float b) {return BitConverter.ToInt32(BitConverter.GetBytes(a),0)==BitConverter.ToInt32(BitConverter.GetBytes(b),0);}
        float Current(int offset) {return BitConverter.ToSingle(Read(handle,manager+(ulong)offset,4),0);}
        void Write(int offset,float value) {
            ulong target=manager+(ulong)offset;
            if(target%4!=0)throw new InvalidOperationException("Alignement camera invalide.");
            Region r;
            if(VirtualQueryEx(handle,new IntPtr((long)target),out r,(UIntPtr)48)==UIntPtr.Zero ||
               r.State!=0x1000 || r.Type!=0x20000 || r.Protect!=2)
                throw new InvalidOperationException("Protection camera inattendue : ecriture refusee.");
            uint old;
            if(!VirtualProtectEx(handle,new IntPtr((long)target),(UIntPtr)4,4,out old))throw Failure("Acces temporaire au reglage refuse");
            bool restored=false;
            try {
                UIntPtr count;byte[] b=BitConverter.GetBytes(value);
                if(!WriteProcessMemory(handle,new IntPtr((long)target),b,(UIntPtr)4,out count) || count.ToUInt64()!=4)throw Failure("Ecriture du reglage echouee");
                if(!Same(Current(offset),value))throw new InvalidOperationException("Verification du reglage echouee.");
            }finally {
                uint ignored;restored=VirtualProtectEx(handle,new IntPtr((long)target),(UIntPtr)4,old,out ignored);
                if(!restored)throw Failure("Restauration de protection echouee : fermez le jeu");
            }
        }
        internal void Apply(float horizontal,float height,bool aiming) {
            ApplyProfiles(new CameraProfile(true,horizontal,height),new CameraProfile(aiming,horizontal,height),new CameraProfile(aiming,horizontal,height));
        }
        static void ValidateProfile(CameraProfile profile) {
            if(!Finite(profile.Horizontal) || !Finite(profile.Height) || Math.Abs(profile.Horizontal)>1.5f || Math.Abs(profile.Height)>0.75f)
                throw new ArgumentOutOfRangeException("Reglages hors limites.");
        }
        internal void ApplyProfiles(CameraProfile exploration,CameraProfile bow,CameraProfile staff) {
            ValidateProfile(exploration);ValidateProfile(bow);ValidateProfile(staff);
            Ensure();var desired=new Dictionary<int,float>();
            foreach(var entry in last)desired[entry.Key]=BitConverter.ToSingle(original,entry.Key);
            if(exploration.Enabled)foreach(int state in Basic)SetDesired(desired,state,exploration.Horizontal,exploration.Height);
            if(bow.Enabled)foreach(int state in Bow)SetDesired(desired,state,bow.Horizontal,bow.Height);
            if(staff.Enabled)foreach(int state in Staff)SetDesired(desired,state,staff.Horizontal,staff.Height);
            // Verify the whole transaction before the first write; never overwrite another mod's changes.
            foreach(var entry in last)if(!Same(Current(entry.Key),entry.Value))throw new InvalidOperationException("Un autre changement de camera a ete detecte. Reglages suspendus.");
            try {
                foreach(var entry in desired) {
                    if(Same(last[entry.Key],entry.Value))continue;
                    last[entry.Key]=entry.Value;dirty.Add(entry.Key);
                    Write(entry.Key,entry.Value);
                }
            }catch {try {Restore();}catch {}throw;}
        }
        void SetDesired(Dictionary<int,float> values,int state,float horizontal,float height) {
            int p=state*Stride+884;values[p]=horizontal;values[p+12]=BitConverter.ToSingle(original,p+12)+height;
        }
        internal void Restore() {
            if(dirty.Count==0)return;
            if(!IsAlive){dirty.Clear();return;}
            Ensure();var remaining=new List<int>(dirty);bool conflict=false;
            foreach(int offset in remaining) {
                float before=BitConverter.ToSingle(original,offset),current=Current(offset);
                if(!Same(current,last[offset]) && !Same(current,before)){conflict=true;continue;}
                if(!Same(current,before))Write(offset,before);
                last[offset]=before;dirty.Remove(offset);
            }
            if(conflict)throw new InvalidOperationException("Restauration partielle : un autre outil a change des valeurs.");
        }
        public void Dispose() {
            if(disposed)return;
            try {Restore();}finally {disposed=true;if(handle!=IntPtr.Zero)CloseHandle(handle);handle=IntPtr.Zero;}
        }
        internal static void SelfTest() {
            if(IntPtr.Size!=8 || Marshal.SizeOf(typeof(Region))!=48)throw new Exception("Disposition x64 invalide");
            byte[] b=new byte[Size];for(int i=0;i<Count;i++){b[i*Stride]=(byte)i;Array.Copy(BitConverter.GetBytes(1.75f),0,b,i*Stride+896,4);}
            if(!Plausible(b,0,b.Length))throw new Exception("Validation synthetique echouee");
            IntPtr memory=VirtualAlloc(IntPtr.Zero,(UIntPtr)Size,0x3000,4);
            if(memory==IntPtr.Zero)throw Failure("Allocation de test");
            try {
                Marshal.Copy(b,0,memory,Size);IntPtr h=OpenProcess(0x438,false,Process.GetCurrentProcess().Id);
                if(h==IntPtr.Zero)throw Failure("Handle de test");
                uint old;if(!VirtualProtectEx(h,memory,(UIntPtr)Size,2,out old))throw Failure("Protection de test");
                using(var s=new CameraSession(h,(ulong)memory.ToInt64())) {
                    s.ApplyProfiles(new CameraProfile(true,0.45f,0.2f),new CameraProfile(true,-0.35f,0.1f),new CameraProfile(true,1.05f,-0.15f));
                    if(!Same(s.Current(4*Stride+884),0.45f) || !Same(s.Current(4*Stride+896),1.95f))throw new Exception("Application echouee");
                    if(!Same(s.Current(8*Stride+884),-0.35f) || !Same(s.Current(9*Stride+896),1.85f))throw new Exception("Profil arc incorrect");
                    if(!Same(s.Current(10*Stride+884),1.05f) || !Same(s.Current(13*Stride+896),1.6f))throw new Exception("Profil baton incorrect");
                    if(!Same(s.Current(12*Stride+884),0) || !Same(s.Current(43*Stride+884),0))throw new Exception("Vue non demandee modifiee");
                    s.ApplyProfiles(new CameraProfile(true,0.45f,0.2f),new CameraProfile(false,-0.35f,0.1f),new CameraProfile(true,1.05f,-0.15f));
                    if(!Same(s.Current(8*Stride+884),0) || !Same(s.Current(10*Stride+884),1.05f))throw new Exception("Desactivation de profil incorrecte");
                    bool invalidStaff=false;
                    try{s.ApplyProfiles(new CameraProfile(true,0.9f,0),new CameraProfile(true,0.8f,0),new CameraProfile(true,float.NaN,0));}
                    catch(ArgumentOutOfRangeException){invalidStaff=true;}
                    if(!invalidStaff || !Same(s.Current(4*Stride+884),0.45f))throw new Exception("Transaction invalide partiellement appliquee");
                    Region r;VirtualQueryEx(h,memory,out r,(UIntPtr)48);if(r.Protect!=2)throw new Exception("Protection non restauree");
                    s.Restore();byte[] after=Read(h,(ulong)memory.ToInt64(),Size);
                    for(int i=0;i<Size;i++)if(after[i]!=b[i])throw new Exception("Octet non restaure");
                    bool invalid=false;try{s.Apply(float.NaN,0,false);}catch(ArgumentOutOfRangeException){invalid=true;}
                    if(!invalid)throw new Exception("NaN accepte");
                    // Simulate an external conflict without touching any game process.
                    s.Write(4*Stride+884,0.9f);bool refused=false;
                    try{s.Apply(0.4f,0,false);}catch(InvalidOperationException){refused=true;}
                    if(!refused || !Same(s.Current(4*Stride+884),0.9f))throw new Exception("Conflit non respecte");
                    s.Write(4*Stride+884,0);
                }
                Console.WriteLine("PASS: independent exploration/bow/staff profiles, opt-out restoration, untouched other cameras, transaction validation, protection restoration, exact rollback and conflict refusal. No game accessed.");
            }finally {VirtualFree(memory,UIntPtr.Zero,0x8000);}
        }
    }
}
