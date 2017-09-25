import csv
import json
import time

def convert_date(datetime_string):
    dt = time.strptime(datetime_string, "%d.%m.%Y %H:%M")
    return time.strftime("%Y-%m-%d", dt)

def convert_person(name_string):
    names = name_string.split(", ")
    return {"first_name": names[1], "last_name": names[0]}

def synonyms(base):
    return [base, base.upper(), base.lower()]

def synonym_data(items):
    expanded_synonyms = [synonyms(i) for i in items]
    single_synonyms = [[i[0], j] for i in expanded_synonyms for j in i[1:]]
    return "\n".join([json.dumps(i) for i in single_synonyms])

with open('absences.csv', 'r') as f:
    reader  = csv.reader(f, delimiter=';')
    raw_absences = list(reader)

absences = raw_absences[1:]
absences = list(map(lambda a: {"person": convert_person(a[4]),
                               "type": a[7],
                               "start": convert_date(a[11]),
                               "end": convert_date(a[12])}, absences))
data = "\n".join(list(map(lambda a: json.dumps(a), absences)))

first_names = set([a["person"]["first_name"] for a in absences])
last_names = set([a["person"]["last_name"] for a in absences])
absence_types = set([a["type"] for a in absences])

first_name_syns = synonym_data(first_names)
last_name_syns = synonym_data(last_names)
absence_type_syns = synonym_data(absence_types)

with open('data.json', 'w') as f:
    f.write(data)
with open('first_name.syn', 'w') as f:
    f.write(first_name_syns)
with open('last_name.syn', 'w') as f:
    f.write(last_name_syns)
with open('type.syn', 'w') as f:
    f.write(absence_type_syns)

print(json)
print(first_name_syns)
print(last_name_syns)
print(absence_type_syns)