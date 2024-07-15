import json
import os
import logging
import yaml
import argparse
from typing import Dict, Any, Tuple

class BaseParser:
    def __init__(self, paths: Tuple[str, str]):
        self.path, self.errors_path = paths

    def get_files_paths(self) -> list:
        return [os.path.join(dirpath, filename) 
                for dirpath, _, filenames in os.walk(self.path) 
                for filename in filenames]

    @staticmethod
    def check_file_extension(path: str, extension: str) -> bool:
        return path.endswith(extension)

class YMLParser(BaseParser):
    def __init__(self, paths: Tuple[str, str]):
        super().__init__(paths)
        logging.basicConfig(filename=self.errors_path, level=logging.ERROR)

    @staticmethod
    def check_proto_attrs(prototype: Dict[str, Any]) -> bool:
        attrs_lst = ["name", "description", "suffix"]
        if not isinstance(prototype.get("parent"), list):
            attrs_lst.append("parent")
        return any(prototype.get(attr) is not None for attr in attrs_lst)

    @staticmethod
    def get_proto_attrs(prototypes: Dict[str, Dict[str, Any]], prototype: Dict[str, Any]) -> None:
        proto_id = prototype.get("id")
        if proto_id:
            prototypes[proto_id] = {
                "parent": prototype.get("parent"),
                "name": prototype.get("name"),
                "desc": prototype.get("description"),
                "suffix": prototype.get("suffix")
            }
        else:
            logging.warning(f"YML-WARNING: Prototype without id: {prototype}")

    @staticmethod
    def create_proto(file) -> str:
        return ''.join(line for line in file if not line.strip().startswith("!type"))

    def yml_parser(self) -> Dict[str, Dict[str, Any]]:
        prototypes = {}
        for path in self.get_files_paths():
            if not self.check_file_extension(path, ".yml"):
                continue
            try:
                with open(path, encoding="utf-8") as file:
                    proto = self.create_proto(file)
                    data = yaml.safe_load(proto)
                if data:
                    for prototype in data:
                        if self.check_proto_attrs(prototype):
                            self.get_proto_attrs(prototypes, prototype)
                        else:
                            logging.warning(f"YML-WARNING: Incomplete prototype in {path}: {prototype}")
            except yaml.YAMLError as ye:
                logging.error(f"YML-ERROR: YAML parsing error in {path}. Error: {str(ye)}")
            except Exception as e:
                logging.error(f"YML-ERROR: An error occurred during prototype processing {path}. Error: {str(e)}")
        return prototypes

class FTLParser(BaseParser):
    def __init__(self, paths: Tuple[str, str]):
        super().__init__(paths)
        logging.basicConfig(filename=self.errors_path, level=logging.ERROR)

    def ftl_parser(self) -> Dict[str, Dict[str, Any]]:
        prototypes = {}
        for path in self.get_files_paths():
            if not self.check_file_extension(path, ".ftl"):
                continue
            try:
                file_prototypes = self.read_ftl((path, self.errors_path))
                if file_prototypes is not None and isinstance(file_prototypes, dict):
                    prototypes.update(file_prototypes)
                else:
                    logging.warning(f"FTL-WARNING: Invalid or empty prototypes returned from {path}")
            except Exception as e:
                logging.error(f"FTL-ERROR: An error occurred while processing {path}: {str(e)}")
        if not prototypes:
            logging.warning("FTL-WARNING: No valid prototypes were parsed from any FTL files.")
        return prototypes

    @staticmethod
    def read_ftl(paths: Tuple[str, str]) -> Dict[str, Dict[str, Any]]:
        prototypes = {}
        current_prototype = None
        current_attribute = None
        path, error_log_path = paths
        logging.basicConfig(filename=error_log_path, level=logging.ERROR)
        try:
            with open(path, encoding="utf-8") as file:
                for line_num, line in enumerate(file, 1):
                    line = line.rstrip()
                    if not line or line.startswith("#"):
                        continue
                    try:
                        if not line.startswith(" "):
                            # New prototype
                            if "=" in line:
                                proto_id, proto_content = line.split("=", 1)
                                proto_id = proto_id.strip().replace("ent-", "")
                                current_prototype = proto_id
                                prototypes[proto_id] = {"name": proto_content.strip()}
                                current_attribute = "name"
                            else:
                                logging.warning(f"FTL-WARNING: Invalid prototype definition in {path}, line {line_num}: {line}")
                        else:
                            # Continuation of previous prototype or new attribute
                            if current_prototype is None:
                                logging.warning(f"FTL-WARNING: Content without prototype in {path}, line {line_num}: {line}")
                                continue
                            
                            if line.lstrip().startswith("."):
                                # New attribute
                                attr, content = line.lstrip().split("=", 1)
                                current_attribute = attr.strip()[1:]  # Remove the leading dot
                                prototypes[current_prototype][current_attribute] = content.strip()
                            else:
                                # Continuation of previous attribute
                                if current_attribute is None:
                                    logging.warning(f"FTL-WARNING: Content without attribute in {path}, line {line_num}: {line}")
                                    continue
                                prototypes[current_prototype][current_attribute] += "\n" + line.strip()
                    except ValueError as ve:
                        logging.error(f"FTL-ERROR: Invalid format in {path}, line {line_num}: {line}. Error: {ve}")
                    except KeyError as ke:
                        logging.error(f"FTL-ERROR: KeyError in {path}, line {line_num}: {line}. Error: {ke}")
        except FileNotFoundError:
            logging.error(f"FTL-ERROR: File not found: {path}")
        except Exception as e:
            logging.error(f"FTL-ERROR: An error occurred while reading file {path}. Error: {str(e)}")
        return prototypes

# Update the create_ftl function to handle multi-line content
def create_ftl(key: str, prototype: Dict[str, Any]) -> str:
    try:
        ftl = f"ent-{key} = {prototype.get('name', '')}\n"
        for attr, value in prototype.items():
            if attr != 'name' and value is not None:
                if '\n' in value:
                    # Multi-line content
                    ftl += f"    .{attr} =\n"
                    for line in value.split('\n'):
                        ftl += f"        {line}\n"
                else:
                    # Single-line content
                    ftl += f"    .{attr} = {value}\n"
        ftl += "\n"
        return ftl
    except Exception as e:
        logging.error(f"Error in create_ftl for key {key}: {str(e)}")
        return ""

def read_config(config_path: str) -> Dict[str, Any]:
    try:
        with open(config_path, "r", encoding="utf-8") as file:
            return json.load(file)
    except FileNotFoundError:
        print(f"Error: {config_path} not found. Please ensure the file exists.")
        return None
    except json.JSONDecodeError:
        print(f"Error: Invalid JSON in {config_path}. Please check the file format.")
        return None

def get_paths(config: Dict[str, Any]) -> Tuple[str, str, str, str]:
    try:
        return (
            config["paths"]["prototypes"],
            config["paths"]["localization"],
            config["paths"]["error_log_path"],
            config["paths"]["yml_parser_last_launch"]
        )
    except KeyError as e:
        print(f"Error: Missing key in config: {str(e)}")
        return None

def print_errors_log_info(errors_log_path: str, all_prototypes: Dict[str, Any]) -> None:
    try:
        with open(errors_log_path, "r") as file:
            errors = file.read()
        successful_count = len(all_prototypes) - errors.count("ERROR")
        print(f"""Of the {len(all_prototypes)} prototypes, {successful_count} were successfully processed.
Errors can be found in  {errors_log_path}
Number of errors during YML processing - {errors.count("YML-ERROR")}
Number of errors during FTL processing - {errors.count("FTL-ERROR")}
Number of errors during data extraction and creation of new FTL  - {errors.count("RETRIEVING-ERROR")}""")
    except Exception as e:
        print(f"Error while printing error log info: {str(e)}")

def check_changed_attrs(yml_parser_last_launch: str, prototypes_dict: Dict[str, Any], localization_dict: Dict[str, Any]):
    try:
        if os.path.isfile(yml_parser_last_launch):
            with open(yml_parser_last_launch, 'r', encoding='utf-8') as file:
                last_launch_prototypes = json.load(file)
            for prototype, proto_attrs_in_prototypes in prototypes_dict.items():
                if prototype in last_launch_prototypes and prototype in localization_dict:
                    attrs = localization_dict[prototype]
                    last_launch_prototype_attrs = last_launch_prototypes[prototype]
                    for key, value in proto_attrs_in_prototypes.items():
                        if value != last_launch_prototype_attrs.get(key):
                            attrs[key] = value
                    localization_dict[prototype] = attrs
    except Exception as e:
        logging.error(f"Error in check_changed_attrs: {str(e)}")

def save_result(entities: str, file_name: str) -> None:
    try:
        with open(file_name, "w", encoding="utf-8") as file:
            file.write(entities)
        print(f"{file_name} has been created\n")
    except Exception as e:
        print(f"Error while saving result to {file_name}: {str(e)}")

def parse_and_process(config_path: str):
    logging.basicConfig(filename="script_execution.log", level=logging.ERROR)
    
    config = read_config(config_path)
    if not config:
        return

    paths = get_paths(config)
    if not paths:
        return

    prototypes_path, localization_path, errors_log_path, yml_parser_last_launch = paths

    if not os.path.isdir("last_launch"):
        os.mkdir("last_launch")
    
    open(errors_log_path, "w").close()  # Clear the error log

    try:
        yml_parser = YMLParser((prototypes_path, errors_log_path))
        prototypes_dict = yml_parser.yml_parser()

        ftl_parser = FTLParser((localization_path, errors_log_path))
        localization_dict = ftl_parser.ftl_parser()

        check_changed_attrs(yml_parser_last_launch, prototypes_dict, localization_dict)

        with open(yml_parser_last_launch, 'w') as json_file:
            json.dump(prototypes_dict, json_file, indent=4)

        all_prototypes = {**prototypes_dict, **localization_dict}
        entities_ftl = ""

        for prototype, prototype_attrs in all_prototypes.items():
            try:
                if prototype in prototypes_dict:
                    prototype_attrs["parent"] = prototypes_dict[prototype].get("parent")
                    parent = prototype_attrs["parent"]
                    if not isinstance(parent, list) and parent in prototypes_dict:
                        if not prototype_attrs.get("name"):
                            prototype_attrs["name"] = f"{{ ent-{parent} }}"
                        if not prototype_attrs.get("desc"):
                            if parent and not isinstance(parent, list) and prototypes_dict.get(parent):
                                prototype_attrs["desc"] = f"{{ ent-{parent}.desc }}"
                    if not prototype_attrs.get("suffix"):
                        if prototypes_dict[prototype].get("suffix"):
                            prototype_attrs["suffix"] = prototypes_dict[prototype]["suffix"]
                
                if any(prototype_attrs.get(attr) is not None for attr in ["name", "desc", "suffix"]):
                    proto_ftl = create_ftl(prototype, prototype_attrs)
                    entities_ftl += proto_ftl
            except Exception as e:
                logging.error(f"RETRIEVING-ERROR: Error processing prototype {prototype}: {str(e)}")

        save_result(entities_ftl, "entities.ftl")
        print_errors_log_info(errors_log_path, all_prototypes)

    except Exception as e:
        logging.error(f"An unexpected error occurred: {str(e)}")
        print("An unexpected error occurred. Check the script_execution.log for details.")

def list_prototypes(config_path: str):
    config = read_config(config_path)
    if not config:
        return

    paths = get_paths(config)
    if not paths:
        return

    prototypes_path, _, errors_log_path, _ = paths

    yml_parser = YMLParser((prototypes_path, errors_log_path))
    prototypes_dict = yml_parser.yml_parser()

    print("List of prototypes:")
    for proto_id, proto_data in prototypes_dict.items():
        print(f"ID: {proto_id}")
        print(f"  Name: {proto_data.get('name', 'N/A')}")
        print(f"  Description: {proto_data.get('desc', 'N/A')}")
        print(f"  Suffix: {proto_data.get('suffix', 'N/A')}")
        print(f"  Parent: {proto_data.get('parent', 'N/A')}")
        print()

def search_prototype(config_path: str, search_term: str):
    config = read_config(config_path)
    if not config:
        return

    paths = get_paths(config)
    if not paths:
        return

    prototypes_path, _, errors_log_path, _ = paths

    yml_parser = YMLParser((prototypes_path, errors_log_path))
    prototypes_dict = yml_parser.yml_parser()

    print(f"Search results for '{search_term}':")
    for proto_id, proto_data in prototypes_dict.items():
        if search_term.lower() in proto_id.lower() or \
           search_term.lower() in str(proto_data).lower():
            print(f"ID: {proto_id}")
            print(f"  Name: {proto_data.get('name', 'N/A')}")
            print(f"  Description: {proto_data.get('desc', 'N/A')}")
            print(f"  Suffix: {proto_data.get('suffix', 'N/A')}")
            print(f"  Parent: {proto_data.get('parent', 'N/A')}")
            print()

def main():
    parser = argparse.ArgumentParser(description="Prototype Parser and Processor")
    parser.add_argument("--config", default="config.json", help="Path to the configuration file")
    parser.add_argument("--action", choices=["parse", "list", "search"], default="parse", help="Action to perform")
    parser.add_argument("--search-term", help="Search term for prototype search")

    args = parser.parse_args()

    if args.action == "parse":
        parse_and_process(args.config)
    elif args.action == "list":
        list_prototypes(args.config)
    elif args.action == "search":
        if not args.search_term:
            print("Error: --search-term is required when using the search action")
            return
        search_prototype(args.config, args.search_term)

if __name__ == '__main__':
	main()