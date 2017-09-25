iex 'rm -Force -ErrorAction SilentlyContinue data\absences.index'
iex 'rm -Force -ErrorAction SilentlyContinue data\absences.grammar'

iex 'kes.exe build_index schema.json data\data.json data\absences.index'
iex 'kes.exe build_grammar grammar.xml data\absences.grammar'
iex 'kes.exe host_service data\absences.grammar data\absences.index'