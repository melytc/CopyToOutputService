namespace CopyToOutputSevice
{
    internal interface ICopyItemService
    {
        // Storing data

        void StoreCopyItemsForProject(ProjectIdentity id, bool isEnabled, ImmutableArray<CopyItem> items, ImmutableArray<ProjectIdentity> referencedProjects);

        // Querying data

        IEnumerable<CopyItem>? TryGatherCopyItemsForProject(ProjectIdentity id, Logger logger);
    }

    // Identifies a configured project
    class ProjectIdentity { string Path {get;} ProjectConfiguration Configuration {get;} }

    class CopyItem
    {
        // absolute paths
        string FromPath {get;}
        string ToPath {get;}
    }

    class UnitTests
    {
        // We will return a null value when any of the following scenarios is true:
        //      - FromProject is unregistered. i.e. A -> B, A is unregistered.
        //      - ToProject is unregistered. i.e. A -> B, B is unregistered.
        //      - ToProject is disabled. i.e. A -> B, B is disabled.
        void ICopyItemServiceReturnsNull()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());

            // ProjectA is unregistered.
            Assert.Equal(null, TryGatherCopyItemsForProject(ProjectA, new Logger()));

            // Store ProjectA with no copy items nor referenced projects. ProjectB is unregistered.
            service.StoreCopyItemsForProject(ProjectA, true, new ImmutableArray<CopyItem>(), new ImmutableArray<ProjectIdentity>());
            Assert.Equal(null, TryGatherCopyItemsForProject(ProjectA, new Logger()));

            // ProjectB is stored but disabled.
            service.StoreCopyItemsForProject(ProjectB, false, new ImmutableArray<CopyItem>(), new ImmutableArray<ProjectIdentity>());
            Assert.Equal(null, TryGatherCopyItemsForProject(ProjectB, new Logger()));
        }

        // We will return an empty IEnumerable<CopyItem> when neither FromProject nor ToProject has items to be copied.
        void ICopyItemServiceReturnsEmpty()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());

            IEnumerable<CopyItem> CopyItemsA = null;
            IEnumerable<CopyItem> CopyItemsB = null;
            
            service.StoreCopyItemsForProject(ProjectA, true, CopyItemsA, new ImmutableArray<ProjectIdentity>(){ ProjectB });
            service.StoreCopyItemsForProject(ProjectB, true, CopyItemsB, new ImmutableArray<ProjectIdentity>(){ ProjectA });

            Assert.Equal(IEnumerable.Empty<CopyItem>, TryGatherCopyItemsForProject(ProjectA, new Logger()));
            Assert.Equal(IEnumerable.Empty<CopyItem>, TryGatherCopyItemsForProject(ProjectB, new Logger()));
        }

        void ICopyItemServiceReturnsItemsWhenTheyExist()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());

            IEnumerable<CopyItem> CopyItemsA = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder1\file1A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder1\file2A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2") };

            IEnumerable<CopyItem> CopyItemsB = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder1\file1A.cs", ToPath:"C:\Test\Path\ProjectA\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder1\file2A.cs", ToPath:"C:\Test\Path\ProjectA\Folder2") };
            
            service.StoreCopyItemsForProject(ProjectA, true, CopyItemsA, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectB, true, CopyItemsB, new ImmutableArray<ProjectIdentity>());

            Assert.Equal(CopyItemsA, TryGatherCopyItemsForProject(ProjectA, new Logger()));
            Assert.Equal(CopyItemsB, TryGatherCopyItemsForProject(ProjectB, new Logger()));
        }

        void ICopyItemServiceReturnsConcatenatedItems()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());

            IEnumerable<CopyItem> CopyItemsA = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder1\file1A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder1\file2A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2") };

            IEnumerable<CopyItem> CopyItemsB = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder1\file1B.cs", ToPath:"C:\Test\Path\ProjectA\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder1\file2B.cs", ToPath:"C:\Test\Path\ProjectA\Folder2") };
            
            IEnumerable<CopyItem> ConcatItems = CopyItemsA.Concat(CopyItemsB);
            
            // A -> B
            service.StoreCopyItemsForProject(ProjectA, true, CopyItemsA, new ImmutableArray<ProjectIdentity>(){ ProjectB });
            service.StoreCopyItemsForProject(ProjectB, true, CopyItemsB, new ImmutableArray<ProjectIdentity>());

            Assert.Equal(ConcatItems, TryGatherCopyItemsForProject(ProjectA, new Logger())); // Only ProjectA has items.
            Assert.Equal(CopyItemsB, TryGatherCopyItemsForProject(ProjectB, new Logger()));
        }

        void ICopyItemService_RecursiveReferences()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());
            ProjectIdentity ProjectC = new ProjectIdentity("C:\Test\Path\ProjectC", new ProjectConfiguration());
            ProjectIdentity ProjectD = new ProjectIdentity("C:\Test\Path\ProjectD", new ProjectConfiguration());

            IEnumerable<CopyItem> CopyItemsA = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder\file1A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder\file2A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2") };

            IEnumerable<CopyItem> CopyItemsB = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder\fileB.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> CopyItemsC = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectC\Folder\fileC.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> CopyItemsD = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectD\Folder\fileD.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> ConcatItems = CopyItemsA.Concat(CopyItemsB).Concat(CopyItemsC).Concat(CopyItemsD);

            // A -> B -> C -> D
            // Q: do we need to store project in a specific order (i.e. topological)?
            service.StoreCopyItemsForProject(ProjectD, true, CopyItemsD, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectC, true, CopyItemsC, new ImmutableArray<ProjectIdentity>(){ ProjectD });
            service.StoreCopyItemsForProject(ProjectB, true, CopyItemsB, new ImmutableArray<ProjectIdentity>(){ ProjectC });
            service.StoreCopyItemsForProject(ProjectA, true, CopyItemsA, new ImmutableArray<ProjectIdentity>(){ ProjectB });

            Assert.Equal(ConcatItems, TryGatherCopyItemsForProject(ProjectA, new Logger())); // Only ProjectA has items.
            Assert.Equal(CopyItemsB.Concat(CopyItemsC).Concat(CopyItemsD), TryGatherCopyItemsForProject(ProjectB, new Logger()));
            Assert.Equal(CopyItemsC.Concat(CopyItemsD), TryGatherCopyItemsForProject(ProjectC, new Logger()));
            Assert.Equal(CopyItemsD, TryGatherCopyItemsForProject(ProjectD, new Logger()));
        }

        void ICopyItemService_References()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());
            ProjectIdentity ProjectC = new ProjectIdentity("C:\Test\Path\ProjectC", new ProjectConfiguration());
            ProjectIdentity ProjectD = new ProjectIdentity("C:\Test\Path\ProjectD", new ProjectConfiguration());

            IEnumerable<CopyItem> CopyItemsA = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder\file1A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder\file2A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2") };

            IEnumerable<CopyItem> CopyItemsB = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder\fileB.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> CopyItemsC = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectC\Folder\fileC.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> CopyItemsD = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectD\Folder\fileD.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> ConcatItemsA = CopyItemsA.Concat(CopyItemsB).Concat(CopyItemsC).Concat(CopyItemsD);

            IEnumerable<CopyItem> ConCatItemsC = CopyItemsC.Concat(CopyItemsD);

            // A -> B
            // A -> C -> D
            service.StoreCopyItemsForProject(ProjectD, true, CopyItemsD, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectC, true, CopyItemsC, new ImmutableArray<ProjectIdentity>(){ ProjectD });
            service.StoreCopyItemsForProject(ProjectB, true, CopyItemsB, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectA, true, CopyItemsA, new ImmutableArray<ProjectIdentity>(){ ProjectB, ProjectC });

            Assert.Equal(ConcatItemsA, TryGatherCopyItemsForProject(ProjectA, new Logger())); // Only ProjectA has all items.
            Assert.Equal(CopyItemsB, TryGatherCopyItemsForProject(ProjectB, new Logger()));
            Assert.Equal(ConCatItemsC, TryGatherCopyItemsForProject(ProjectC, new Logger())); // ProjectC has items of C and D.
            Assert.Equal(CopyItemsD, TryGatherCopyItemsForProject(ProjectD, new Logger()));
        }

        void ICopyItemService_FlatReferences()
        {
            var service = new ICopyItemService();

            ProjectIdentity ProjectA = new ProjectIdentity("C:\Test\Path\ProjectA", new ProjectConfiguration());
            ProjectIdentity ProjectB = new ProjectIdentity("C:\Test\Path\ProjectB", new ProjectConfiguration());
            ProjectIdentity ProjectC = new ProjectIdentity("C:\Test\Path\ProjectC", new ProjectConfiguration());
            ProjectIdentity ProjectD = new ProjectIdentity("C:\Test\Path\ProjectD", new ProjectConfiguration());

            IEnumerable<CopyItem> CopyItemsA = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder\file1A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2"),
                new CopyItem(FromPath: "C:\Test\Path\ProjectA\Folder\file2A.cs", ToPath:"C:\Test\Path\ProjectB\Folder2") };

            IEnumerable<CopyItem> CopyItemsB = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectB\Folder\fileB.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> CopyItemsC = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectC\Folder\fileC.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> CopyItemsD = new []{ 
                new CopyItem(FromPath: "C:\Test\Path\ProjectD\Folder\fileD.cs", ToPath:"C:\Test\Path\ProjectA\Folder") };

            IEnumerable<CopyItem> ConcatItemsA = CopyItemsA.Concat(CopyItemsB).Concat(CopyItemsC).Concat(CopyItemsD);

            // A -> B
            // A -> C
            // A -> D
            service.StoreCopyItemsForProject(ProjectD, true, CopyItemsD, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectC, true, CopyItemsC, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectB, true, CopyItemsB, new ImmutableArray<ProjectIdentity>());
            service.StoreCopyItemsForProject(ProjectA, true, CopyItemsA, new ImmutableArray<ProjectIdentity>(){ ProjectB, ProjectC, ProjectD });

            Assert.Equal(ConcatItemsA, TryGatherCopyItemsForProject(ProjectA, new Logger())); // Only ProjectA has all items.
            Assert.Equal(CopyItemsB, TryGatherCopyItemsForProject(ProjectB, new Logger()));
            Assert.Equal(CopyItemsC, TryGatherCopyItemsForProject(ProjectC, new Logger()));
            Assert.Equal(CopyItemsD, TryGatherCopyItemsForProject(ProjectD, new Logger()));
        }
    }

}
