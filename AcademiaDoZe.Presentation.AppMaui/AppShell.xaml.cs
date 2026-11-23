// Francis Miguel Kuhl Schweitzer Junior
using AcademiaDoZe.Presentation.AppMaui.Views;

namespace AcademiaDoZe.Presentation.AppMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Registrar rotas das páginas de cadastro/edição
        Routing.RegisterRoute("logradouro", typeof(LogradouroPage));
        Routing.RegisterRoute("colaborador", typeof(ColaboradorPage));
        Routing.RegisterRoute("aluno", typeof(AlunoPage));
        Routing.RegisterRoute("matricula", typeof(MatriculaPage));
    }
}