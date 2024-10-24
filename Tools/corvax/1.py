import os
import re
from pathlib import Path

def parse_ftl_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        content = file.read()
    
    entries = {}
    current_key = None
    for line in content.split('\n'):
        if line.strip() and not line.strip().startswith('#'):
            if '=' in line and not line.strip().startswith('.'):
                key, value = line.split('=', 1)
                current_key = key.strip()
                entries[current_key] = {'value': value.strip()}
            elif current_key and (line.strip().startswith('.suffix') or line.strip().startswith('.desc')):
                attr, value = line.strip().split('=', 1)
                entries[current_key][attr.strip('.')] = value.strip()
    
    return entries

def update_ftl_files(source_dir, target_dir, missing_keys_file):
    source_entries = {}
    for root, _, files in os.walk(source_dir):
        for file in files:
            if file.endswith('.ftl'):
                file_path = os.path.join(root, file)
                relative_path = os.path.relpath(file_path, source_dir)
                source_entries[relative_path] = parse_ftl_file(file_path)

    missing_keys = []
    for root, _, files in os.walk(target_dir):
        for file in files:
            if file.endswith('.ftl'):
                target_file_path = os.path.join(root, file)
                relative_path = os.path.relpath(target_file_path, target_dir)
                
                if relative_path in source_entries:
                    with open(target_file_path, 'r', encoding='utf-8') as file:
                        target_content = file.readlines()
                    
                    updated_content = []
                    current_key = None
                    for line in target_content:
                        stripped_line = line.strip()
                        if '=' in stripped_line and not stripped_line.startswith('.'):
                            key, _ = stripped_line.split('=', 1)
                            current_key = key.strip()
                            if current_key in source_entries[relative_path]:
                                updated_value = source_entries[relative_path][current_key]['value']
                                updated_content.append(f"{current_key} = {updated_value}\n")
                            else:
                                updated_content.append(line)
                                missing_keys.append((current_key, target_file_path))
                        elif current_key and (stripped_line.startswith('.suffix') or stripped_line.startswith('.desc')):
                            attr = stripped_line.split('=')[0].strip().strip('.')
                            if current_key in source_entries[relative_path] and attr in source_entries[relative_path][current_key]:
                                updated_value = source_entries[relative_path][current_key][attr]
                                updated_content.append(f"{line.split('=')[0]}= {updated_value}\n")
                            else:
                                updated_content.append(line)
                        else:
                            updated_content.append(line)
                    
                    with open(target_file_path, 'w', encoding='utf-8') as file:
                        file.writelines(updated_content)

    with open(missing_keys_file, 'w', encoding='utf-8') as file:
        for key, file_path in missing_keys:
            file.write(f"{key}: {file_path}\n")

if __name__ == "__main__":
    source_dir = r"D:\OtherGames\SpaceStation14\nuclear-14\Resources\Locale\ru-RU\ss14-ru\prototypes\nuclear14"
    target_dir = r"D:\OtherGames\SpaceStation14\nuclear-14\Resources\Locale\ru-RU\ss14-ru\prototypes\_nuclear14"
    missing_keys_file = "missing_keys.txt"
    
    update_ftl_files(source_dir, target_dir, missing_keys_file)
    print("Обновление файлов завершено. Проверьте файл missing_keys.txt для списка отсутствующих ключей.")