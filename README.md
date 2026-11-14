Sistema de Alerta Precoce de Seca e Chuvas — Mabote e Govuro

Aplicação web para monitorização de dados climáticos e alerta precoce de seca nas regiões de Mabote e Govuro, província de Inhambane, Moçambique. O sistema importa observações climáticas, treina um modelo de regressão para previsão de precipitação e classifica o risco de seca por região.

Funcionalidades

•	Importação de ficheiros CSV com observações de três variáveis: precipitação (CHIRPS), temperatura (ERA5) e humidade do solo (SMAP)
•	Autenticação de utilizadores (ASP.NET Core Identity)
•	Consulta de observações por região e intervalo de datas 
•	Treino de um modelo de regressão (ML.NET) para previsão de precipitação
•	Previsão de precipitação a 7 dias com classificação de risco de seca por região: Normal, Atenção, Alerta, Emergência
•	Página inicial com estatísticas do conjunto de dados

Tecnologias
•	.NET 10 / ASP.NET Core MVC
•	Entity Framework Core (SQL Server)
•	ML.NET (FastForest)
•	Bootstrap 5

Modelo de previsão

•	Alvo: precipitação do dia seguinte, em mm
•	Variáveis de entrada: mês, precipitação com atrasos de 1, 3 e 7 dias, soma de 7 dias, humidade do solo e temperatura
•	Algoritmo: FastForest com 200 árvores
•	Validação: divisão temporal por região — os primeiros 80% dos registos para treino, os últimos 20% para teste, sem sobreposição
•	Métricas: R² e RMSE no conjunto de teste
•	Classificação de risco: comparação entre a precipitação prevista e a média histórica do mês, combinada com a humidade do solo actual

Dados 

O conjunto de dados usado no projecto é sintético, gerado pelo projecto Tools.DataGenerator. A geração segue o padrão climático típico da região de Inhambane — estação chuvosa de Novembro a Março, estação seca de Abril a Outubro — e inclui um período de seca simulada. Os campos de origem indicam a fonte pretendida de cada variável numa versão operacional do sistema:
•	CHIRPS: precipitação estimada por satélite (Climate Hazards Group)
•	ERA5: reanálise climática (ECMWF/Copernicus)
•	SMAP: humidade do solo por satélite (NASA)

Como executar

Requisitos: .NET 10 SDK e SQL Server (LocalDB ou instância completa).
1.	Configurar a ligação à base de dados em “ AI-rainfall-early-warning.Web/appsettings.json”
2.	Compilar e executar no terminal:

   
dotnet build
dotnet run --project AI-rainfall-early-warning.Web

As migrações são aplicadas e os dados iniciais (regiões e utilizador administrador) são criados automaticamente no arranque.
3.	Autenticar-se e importar os ficheiros CSV na página "Dados"
4.	Na página "Previsão", treinar o modelo e consultar a previsão de 7 dias

Limitações conhecidas

•	Os dados são simulados; o modelo aprende padrões do conjunto sintético, não observações reais
•	A previsão é determinística e não produz intervalos de confiança
•	A previsão iterativa de 7 dias acumula incerteza ao longo do horizonte
•	Os limiares de risco são fixos; numa versão operacional deveriam ser calibrados com registos históricos de impactos agrícolas







