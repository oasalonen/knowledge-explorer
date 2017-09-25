import csv
import json
import time

DATA_DIR = "data/"

# Data format of absence CSV file
COLUMN_PERSON = 4
COLUMN_TYPE = 7
COLUMN_START = 11
COLUMN_END = 12

# Keys for KES JSON data
KEY_PERSON = "person"
KEY_TYPE = "type"
KEY_START = "start"
KEY_END = "end"
KEY_FIRST_NAME = "first_name"
KEY_LAST_NAME = "last_name"

def data_path(filename):
    return DATA_DIR + filename

def convert_date(datetime_string):
    dt = time.strptime(datetime_string, "%d.%m.%Y %H:%M")
    return time.strftime("%Y-%m-%d", dt)

def convert_person(name_string):
    names = name_string.split(", ")
    return {KEY_FIRST_NAME: names[1], KEY_LAST_NAME: names[0]}

def synonyms(base):
    return [base, base.upper(), base.lower()]

def synonym_data(items):
    expanded_synonyms = [synonyms(i) for i in items]
    single_synonyms = [[i[0], j] for i in expanded_synonyms for j in i[1:]]
    return "\n".join([json.dumps(i) for i in single_synonyms])

with open(data_path('absences.csv'), 'r') as f:
    reader  = csv.reader(f, delimiter=';')
    raw_absences = list(reader)

# Preprocess input data
absences = raw_absences[1:]
absences = list(map(lambda a: {KEY_PERSON: convert_person(a[COLUMN_PERSON]),
                               KEY_TYPE: a[COLUMN_TYPE],
                               KEY_START: convert_date(a[COLUMN_START]),
                               KEY_END: convert_date(a[COLUMN_END])},
                    absences))
data = "\n".join(list(map(lambda a: json.dumps(a), absences)))

first_names = set([a[KEY_PERSON][KEY_FIRST_NAME] for a in absences])
last_names = set([a[KEY_PERSON][KEY_LAST_NAME] for a in absences])
absence_types = set([a[KEY_TYPE] for a in absences])

# Create synonyms for case insensitive search
first_name_syns = synonym_data(first_names)
last_name_syns = synonym_data(last_names)
absence_type_syns = synonym_data(absence_types)

with open(data_path('data.json'), 'w') as f:
    f.write(data)
with open(data_path('first_name.syn'), 'w') as f:
    f.write(first_name_syns)
with open(data_path('last_name.syn'), 'w') as f:
    f.write(last_name_syns)
with open(data_path('type.syn'), 'w') as f:
    f.write(absence_type_syns)

#print(json)
#print(first_name_syns)
#print(last_name_syns)
#print(absence_type_syns)