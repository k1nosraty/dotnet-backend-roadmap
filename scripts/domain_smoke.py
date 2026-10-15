"""Exercise domain contracts from independent consumer assemblies, without HTTP guards."""
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SAMPLES = ['25-controllers','26-dto-mapping','27-validation','28-error-pipeline','29-options',
           '30-request-scopes','31-rest-pagination','32-openapi','33-supportdesk-api']
common = '''using SupportDesk.Domain;
static void Check(bool accepted, string message)
{
    if (!accepted) throw new InvalidOperationException(message);
}
static void Reject<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}");
}
TicketTitle title = TicketTitle.Create("Network issue");
Reject<ArgumentOutOfRangeException>(() => Ticket.Create(0, title));
Reject<ArgumentOutOfRangeException>(() => Ticket.Create(-1, title));
Reject<ArgumentNullException>(() => Ticket.Create(1, null!));
Ticket original = Ticket.Create(1, title);
Reject<ArgumentNullException>(() => original.Rename(null!));
Ticket changed = original.Rename(TicketTitle.Create("Printer issue"))!;
Check(changed.Id == 1 && changed.Title == "Printer issue", "Rename lost identity or title");
Check(original.Title == "Network issue", "Immutable original was changed");
'''
final = '''
Reject<ArgumentNullException>(() => original.AddNote(null!));
Reject<ArgumentException>(() => original.AddNote("ab"));
Reject<ArgumentException>(() => original.AddNote(new string('x', 501)));
Check(original.ChangeStatus("Closed") is null, "Open closed directly");
Ticket working = original.ChangeStatus("InProgress")!;
Check(working.ChangeStatus("Closed") is null, "Closed without resolution");
Ticket noted = working.AddNote("  Resolution recorded  ")!;
Check(noted.Notes[0] == "Resolution recorded" && working.Notes.Count == 0, "Note copy/normalization failed");
Check(noted.AddNote("Resolution recorded") is null, "Duplicate note accepted");
Reject<NotSupportedException>(() => ((ICollection<string>)noted.Notes).Add("Bypass"));
for (int i = 0; i < 4; i++) noted = noted.AddNote($"Note {i}")!;
Check(noted.Notes.Count == 5 && noted.AddNote("Sixth note") is null, "Capacity failed");
Ticket closed = noted.ChangeStatus("Closed")!;
Check(closed.Rename(title) is null && closed.AddNote("Late note") is null, "Closed ticket edited");
Check(ReferenceEquals(closed, closed.ChangeStatus("Closed")), "Repeated PUT target is not a no-op");
var store = new SupportDesk.Infrastructure.InMemoryTicketStore();
Ticket stored = store.Create(title);
Reject<InvalidOperationException>(() => store.Mutate(stored.Id, _ => Ticket.Create(99, title)));
Check(store.Find(stored.Id) == stored, "Rejected identity change altered the store");
Reject<ArgumentNullException>(() => SupportDesk.Application.MutationResult.Success(null!));
Reject<InvalidOperationException>(() => { _ = SupportDesk.Application.MutationResult.Missing().Value; });
'''
with tempfile.TemporaryDirectory(prefix='supportdesk-domain-contracts-') as directory:
    for i, name in enumerate(SAMPLES):
        folder = Path(directory) / name
        folder.mkdir()
        project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
        props = ET.SubElement(project,'PropertyGroup')
        for key, value in [('OutputType','Exe'),('TargetFramework','net10.0'),('ImplicitUsings','enable'),
                           ('Nullable','enable'),('TreatWarningsAsErrors','true')]:
            ET.SubElement(props,key).text=value
        refs=ET.SubElement(project,'ItemGroup')
        ET.SubElement(refs,'ProjectReference',Include=str(ROOT/'samples'/name/'SupportDesk.csproj'))
        ET.ElementTree(project).write(folder/'Contracts.csproj',encoding='unicode')
        (folder/'Program.cs').write_text(common+(final if i==8 else '')+'\nConsole.WriteLine("PASS: domain contract");\n',encoding='utf-8')
        subprocess.run(['dotnet','run','--project',str(folder/'Contracts.csproj'),'--no-launch-profile'],check=True)
        print(f'PASS: independent consumer of {name}.',flush=True)
print('PASS: positive IDs, null guards, immutable copies and final API domain rules without controller validation.')
