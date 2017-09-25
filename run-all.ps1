iex 'rm .\absences.index'
iex 'rm .\absences.grammar'

iex 'kes.exe build_index schema.json data.json absences.index'
iex 'kes.exe build_grammar grammar.xml absences.grammar'
iex 'kes.exe host_service absences.grammar absences.index'